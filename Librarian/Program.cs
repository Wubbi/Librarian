using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Librarian.Core;
using Librarian.Views;
using queuepacked.ConsoleUI;
// ReSharper disable AccessToDisposedClosure

namespace Librarian
{
    static class Program
    {
        private static readonly Version Version;

        public static Settings Settings { get; private set; }

        private static State State { get; set; }

        static Program()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
            Settings = Settings.Default;
        }

        static async Task Main(string[] args)
        {
            if (args.Length > 0)
                Settings = Settings.CreateOverride(args);

            if (Settings.MigrateOldLibrarianData.Length > 0)
            {
                Migrate();
                return;
            }

            if (Settings.Log)
                Logger.SetLogger(new Logger("log.txt"));

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs)
                => Logger.Instance.Log(eventArgs.ExceptionObject.ToString() ?? "Unknown Exception", Logger.Level.Error);

            State = new State(Settings.LibraryRoot);

            using UiHub uiHub = UiHub.Register(80, 20, true);
            uiHub.Title = $"Librarian v{Version}";
            uiHub.MainLoopInterval = Settings.UiInputInterval;
            uiHub.ViewUpdateReduction = Settings.UiRenderReduction;

            InitView initView = new InitView(uiHub.AddView("Init"));
            MainView mainView = new MainView(uiHub.AddView("Main"));
            DownloadView downloadView = new DownloadView(uiHub.AddView("Download"));
            ScanView scanView = new ScanView(uiHub.AddView("Scan"));

            State.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(State.Current):
                        switch (State.Current)
                        {
                            case State.View.Init:
                                uiHub.SwitchView(initView.Name);
                                break;
                            case State.View.Main:
                                uiHub.SwitchView(mainView.Name);
                                break;
                            case State.View.Donwload:
                                uiHub.SwitchView(downloadView.Name);
                                break;
                            case State.View.Scan:
                                uiHub.SwitchView(scanView.Name);
                                break;
                        }
                        break;
                    case nameof(State.InitCurrentPath):
                        initView.UpdateView(State.InitCurrentPath);
                        break;

                    default:
                        mainView.UpdateView(State);
                        break;
                }
            };

            State.DownloadState.PropertyChanged += (s, e) => downloadView.UpdateView(State.DownloadState);

            State.ScanState.PropertyChanged += (s, e) => scanView.UpdateView(State.ScanState);

            downloadView.UpdateView(State.DownloadState);
            scanView.UpdateView(State.ScanState);

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            Logger.Instance.Log($"Starting Librarian v{Version}");
            Task run = Task.Run(() => Run(tokenSource.Token), tokenSource.Token);
            uiHub.Run();
            tokenSource.Cancel();
            await run;
            Logger.Instance.Log("Stopping");
        }

        private static async Task Run(CancellationToken token)
        {
            try
            {
                State.Local.Scan(p => State.InitCurrentPath = p, token);

                if (Settings.ValidateLibraryOnStartup)
                {
                    //Validate
                    State.Current = State.View.Scan;
                    await State.ScanState.ValidateLibrary(State.Local, token);
                    State.Current = State.View.Main;
                }

                TimeSpan interval = TimeSpan.FromSeconds(Settings.Interval);
                while (!token.IsCancellationRequested)
                {
                    //Look for new version
                    State.UpdateWeb();
                    List<Game> missingVersions = State.Web.Versions.Where(wv =>
                        State.Local.Versions.All(lv =>
                                wv.Id != lv.Id || wv.Type != lv.Type
                            )
                        ).ToList();

                    if (missingVersions.Count > 0)
                        Logger.Instance.Log($"{missingVersions.Count} new versions available");

                    await Download(missingVersions, token);

                    State.Current = State.View.Main;

                    if (Settings.AddedVersionCommand.Length > 0)
                        SignalVersions(missingVersions);

                    State.NextOnlineCheck = DateTime.Now + interval;
                    await Task.Delay(interval, token);
                }
            }
            catch (TaskCanceledException)
            {
                //Expected when stopping the program
                Logger.Instance.Log("Librarian was canceled");
            }
        }

        private static void SignalVersions(List<Game> missingGames)
        {
            if (missingGames.Count == 0)
                return;

            bool useBash = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

            foreach (Game addedVersion in missingGames.OrderBy(g => g.ReleaseTime).Skip(Settings.NewestOnly ? missingGames.Count - 1 : 0))
            {
                try
                {
                    using Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = useBash ? "/bin/bash" : "CMD.exe",
                            Arguments = (useBash ? "-c " : "/C ") + Settings.AddedVersionCommand.Replace("_AddedVersionPath_", State.Local.GetGameFolder(addedVersion)),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }
                    };

                    Logger.Instance.Log($"Running command for version {addedVersion}");
                    process.Start();
                    process.WaitForExit();
                    Logger.Instance.Log($"Command executed; return value {process.ExitCode}");
                }
                catch (Exception e)
                {
                    Logger.Instance.Log(e.ToString(), Logger.Level.Error);
                }

                if (Settings.NewestOnly)
                    break;
            }
        }

        private static async Task Download(List<Game> missingGames, CancellationToken token)
        {
            if (missingGames.Count == 0)
                return;

            State.Current = State.View.Donwload;

            //Plan metadata downloads
            foreach (Game shallowGame in missingGames)
            {
                State.DownloadState.PlanDownload(shallowGame.Url, 0L);

                if (token.IsCancellationRequested)
                    return;
            }

            //Download metadata
            List<Game> missingGamesFull = new List<Game>();
            foreach (Game shallowGame in missingGames)
            {
                Game game = new Game(await State.DownloadState.DownloadStringAsync(shallowGame.Url));
                missingGamesFull.Add(game);

                if (token.IsCancellationRequested)
                    return;
            }

            //Plan jar downloads
            if (!Settings.SkipJars)
            {
                foreach (Game fullGame in missingGamesFull)
                {
                    foreach (Game.DownloadMeta downloadMeta in fullGame.Downloads.Values)
                        State.DownloadState.PlanDownload(downloadMeta.Url, downloadMeta.Size);

                    if (token.IsCancellationRequested)
                        return;
                }
            }

            //Download jars and add games
            foreach (Game missingGame in missingGamesFull)
            {
                Dictionary<Game.AppType, IResource> downloads = new Dictionary<Game.AppType, IResource>();

                if (!Settings.SkipJars)
                {
                    foreach ((Game.AppType appType, Game.DownloadMeta downloadMeta) in missingGame.Downloads)
                    {
                        WebResource download = await State.DownloadState.DownloadAsync(downloadMeta.Url, token);

                        if (token.IsCancellationRequested)
                            return;

                        downloads[appType] = download;
                    }
                }

                State.Local.AddGame(missingGame, downloads);

                foreach (IResource download in downloads.Values)
                    download.Dispose();
            }
        }

        private static void Migrate()
        {
            Console.WriteLine($"Migrating old Librarian data from \"{Settings.MigrateOldLibrarianData}\" to \"{Settings.LibraryRoot}\"");

            LocalInventory localInventory = new LocalInventory(Settings.LibraryRoot);

            foreach (Game.BuildType buildType in Enum.GetValues(typeof(Game.BuildType)).Cast<Game.BuildType>())
            {
                string buildRoot = Path.Combine(Settings.MigrateOldLibrarianData, Game.BuildTypeToString(buildType));

                if (!Directory.Exists(buildRoot))
                    continue;

                foreach (string versions in Directory.EnumerateDirectories(buildRoot))
                {
                    string gameFolder = Directory.EnumerateDirectories(versions).FirstOrDefault();
                    if (gameFolder is null)
                        continue;

                    string fileName = Path.GetFileName(versions);

                    string meta = File.ReadAllText(Path.Combine(gameFolder, fileName + ".json"));

                    Game game = new Game(meta);

                    Dictionary<Game.AppType, IResource> files = new Dictionary<Game.AppType, IResource>();

                    if (File.Exists(Path.Combine(gameFolder, "client.jar")))
                        files[Game.AppType.Client] = LocalResource.Load(Path.Combine(gameFolder, "client.jar"));

                    if (File.Exists(Path.Combine(gameFolder, "server.jar")))
                        files[Game.AppType.Server] = LocalResource.Load(Path.Combine(gameFolder, "server.jar"));

                    localInventory.AddGame(game, files);

                    foreach (IResource resource in files.Values)
                        resource.Data.Dispose();
                }
            }

            Console.WriteLine("Migration complete");
        }
    }
}