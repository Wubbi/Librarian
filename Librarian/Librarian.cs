using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace com.github.Wubbi.Librarian
{
    /// <summary>
    /// Triggers events and maintains the library
    /// </summary>
    public class Librarian : IDisposable
    {
        /// <summary>
        /// A list of subsequent updates on the launcher manifest
        /// </summary>
        private readonly BlockingCollection<LauncherInventory.Diff> _launcherManifestUpdates;

        /// <summary>
        /// The watcher that fires an event on every change of the launchers manifest
        /// </summary>
        private readonly ManifestWatcher _manifestWatcher;

        private readonly ConsolePresenter _consolePresenter;

        private bool _running;

        /// <summary>
        /// The settings this <see cref="Librarian"/> was initialized with
        /// </summary>
        public Settings Settings { get; }

        public Librarian(string settingsFile, bool outputToConsole)
        {
            if (!File.Exists(settingsFile))
                throw new ArgumentException($"Can't find settings \"{settingsFile}\"", nameof(settingsFile));

            if (outputToConsole)
            {
                _consolePresenter = new ConsolePresenter();
                Logger.NewLogEntry += _consolePresenter.AddLogEntry;
            }

            Settings = new Settings(File.ReadAllText(settingsFile));

            if (!Directory.Exists(Settings.LibraryPath))
                Directory.CreateDirectory(Settings.LibraryPath);

            Logger.SetLogFilePath(Path.Combine(Settings.LibraryPath, "log.txt"));

            _launcherManifestUpdates = new BlockingCollection<LauncherInventory.Diff>();

            _manifestWatcher = new ManifestWatcher(Settings.ProcessMissedUpdates ? GetLatestStoredManifest() : null);

            _manifestWatcher.ChangeInLauncherManifest += diff => _launcherManifestUpdates.Add(diff);

            Logger.Info($"Librarian {Assembly.GetExecutingAssembly().GetName().Version} initialized");

            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            if (_consolePresenter != null)
            {
                WebAccess.Instance.DownloadProgressChanged += (o, args) => _consolePresenter.DownloadUpdate = args;
                _manifestWatcher.CheckedManifest += time => _consolePresenter.NextCheck = time.ToString("u");
            }
        }

        private void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Stop();
        }

        public void Dispose()
        {
            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;

            Stop();

            _manifestWatcher?.Dispose();
            _launcherManifestUpdates?.Dispose();

            if (_consolePresenter != null)
            {
                Logger.NewLogEntry -= _consolePresenter.AddLogEntry;
                _consolePresenter.Dispose();
            }
        }

        /// <summary>
        /// This method blocks the current thread in between updates of the launcher manifest. Will perform actions matching a filter for each update.
        /// </summary>
        public void Run()
        {
            _running = true;

            LauncherInventory latestStoredManifest = GetLatestStoredManifest();

            if (_consolePresenter != null && latestStoredManifest != null)
            {
                _consolePresenter.LatestRelease = latestStoredManifest.LatestReleaseId;
                _consolePresenter.LatestSnapshot = latestStoredManifest.LatestSnapshotId;
            }

            if (latestStoredManifest is null)
            {
                Logger.Info("Empty library, setting up initial library state");
                UpdateLibrary(_manifestWatcher.CurrentInventory, !Settings.CheckJarFiles);
                if (_running)
                    Logger.Info("Library set up");
                else
                    return;
            }
            else if (Settings.MaintainInventory)
            {
                UpdateLibrary(latestStoredManifest, !Settings.CheckJarFiles, true);
                if (!_running)
                    return;
            }

            Logger.Info("Starting periodic manifest watch");

            _manifestWatcher.Start(TimeSpan.FromSeconds(Settings.ManifestRefreshRate));

            foreach (LauncherInventory.Diff update in _launcherManifestUpdates.GetConsumingEnumerable())
            {
                Logger.Info("Processing update of manifest");

                if (_consolePresenter != null && latestStoredManifest != null)
                {
                    _consolePresenter.LatestRelease = update.NewInventory.LatestReleaseId;
                    _consolePresenter.LatestSnapshot = update.NewInventory.LatestSnapshotId;
                }

                List<int> completedIds = new List<int>();

                TriggerActions(update, false, ref completedIds);

                UpdateLibrary(update.NewInventory);

                TriggerActions(update, true, ref completedIds);

                if (_running)
                    Logger.Info("Update of manifest processed. Continuing periodic manifest watch");
                else
                    break;
            }

            Logger.Info("Librarian stopped its run");
        }

        /// <summary>
        /// Cancels active downloads and manifest watch
        /// </summary>
        public void Stop()
        {
            if (!_running)
                return;

            _running = false;

            Logger.Info("Stopping Librarian");

            WebAccess.Instance.CancelActiveDownload();

            _launcherManifestUpdates.CompleteAdding();
        }

        /// <summary>
        /// Updates the Library and returns a list of versions that got added
        /// </summary>
        /// <param name="launcherInventory">The inventory the library should update to, or null to reference the latest stored inventory</param>
        /// <param name="metaOnly">If True, only the manifest and metadata but no jar files are downloaded</param>
        /// <returns></returns>
        private void UpdateLibrary(LauncherInventory launcherInventory = null, bool metaOnly = false, bool skipLauncherCheck = false)
        {
            metaOnly = false;
            if (_consolePresenter != null)
            {
                _consolePresenter.LastLibraryUpdate = DateTime.UtcNow.ToString("u");
            }

            if (skipLauncherCheck)
            {
                Logger.Info("Comparing manifest to current library");
                if (launcherInventory is null)
                    launcherInventory = GetLatestStoredManifest();
            }
            else
            {
                Logger.Info("Updating library" + (metaOnly ? " (Metadata only)" : ""));

                string manifestFolder = Path.GetDirectoryName(LauncherInventory.GetManifestFilePath(Settings.LibraryPath));

                LauncherInventory latestStoredManifest = GetLatestStoredManifest();

                if (launcherInventory == null && latestStoredManifest == null)
                {
                    Logger.Info("No manifest found to update to");
                    return;
                }

                if (launcherInventory == null)
                {
                    Logger.Info("Using latest stored manifest");
                    launcherInventory = latestStoredManifest;
                }
                else if (!launcherInventory.Equals(latestStoredManifest))
                {
                    if (!Directory.Exists(manifestFolder))
                        Directory.CreateDirectory(manifestFolder);

                    File.WriteAllText(LauncherInventory.GetManifestFilePath(Settings.LibraryPath), launcherInventory.Manifest);
                    Logger.Info("Added new manifest");
                }
            }

            IEnumerable<GameVersionExtended> missingVersions = GetMissingVersions(launcherInventory, metaOnly, Settings.CheckJarFiles);

            List<GameVersionExtended> processedVersions = new List<GameVersionExtended>();

            foreach (GameVersionExtended version in missingVersions)
            {
                string versionRootPath = Path.Combine(Settings.LibraryPath, version.LibrarySubFolder);

                bool isUpdate = false;

                if (!Directory.Exists(versionRootPath))
                    Directory.CreateDirectory(versionRootPath);
                else
                    isUpdate = true;

                Logger.Info($"{(isUpdate ? "Updating" : "Adding")} {version.Type} {version.Id}");

                string metaDataFilePath = version.GetMetaDataFilePath(Settings.LibraryPath);

                if (!File.Exists(metaDataFilePath))
                    File.WriteAllText(metaDataFilePath, version.MetaData);

                if (!_running)
                {
                    Logger.Info($"{(isUpdate ? "Update" : "Addition")} of {version.Type} {version.Id} was canceled");
                    return;
                }

                string serverPath = Path.Combine(versionRootPath, "server.jar");
                if (!metaOnly && version.ServerDownloadUrl != null && !File.Exists(serverPath))
                {
                    try
                    {
                        Logger.Info($"Downloading server.jar ({version.ServerDownloadSize / 1024.0 / 1024.0:F2} MB)");
                        WebAccess.Instance.DownloadAndStoreFile(version.ServerDownloadUrl, serverPath, version.ServerDownloadSize, version.ServerDownloadSha1);
                        Logger.Info("Download of server.jar complete");
                    }
                    catch (Exception e)
                    {
                        e.Data["File"] = serverPath;
                        Logger.Error("Error while downloading server.jar");
                        Logger.Exception(e);
                    }
                }

                if (!_running)
                {
                    Logger.Info($"{(isUpdate ? "Update" : "Addition")} of {version.Type} {version.Id} was canceled");
                    return;
                }

                string clientPath = Path.Combine(versionRootPath, "client.jar");
                if (!metaOnly && version.ClientDownloadUrl != null && !File.Exists(clientPath))
                {
                    try
                    {
                        Logger.Info($"Downloading client.jar ({version.ClientDownloadSize / 1024.0 / 1024.0:F2} MB)");
                        WebAccess.Instance.DownloadAndStoreFile(version.ClientDownloadUrl, clientPath, version.ClientDownloadSize, version.ClientDownloadSha1);
                        Logger.Info("Download of client.jar complete");
                    }
                    catch (Exception e)
                    {
                        e.Data["File"] = clientPath;
                        Logger.Error("Error while downloading client.jar");
                        Logger.Exception(e);
                    }
                }

                processedVersions.Add(version);
                Logger.Info($"{(isUpdate ? "Update" : "Addition")} of {version.Type} {version.Id} complete");
            }

            switch (processedVersions.Count)
            {
                case 0:
                    if (skipLauncherCheck)
                        Logger.Info("No updates were required");
                    else
                        Logger.Info("No versions were added");
                    break;
                case 1:
                    Logger.Info($"{(skipLauncherCheck ? "Updated" : "Added")} 1 version");
                    break;
                default:
                    Logger.Info($"{(skipLauncherCheck ? "Updated" : "Added")} {processedVersions.Count} versions");
                    break;
            }

            if (skipLauncherCheck)
                Logger.Info("Library is complete");
            else
                Logger.Info("Library is up to date");
        }

        private IEnumerable<GameVersionExtended> GetMissingVersions(LauncherInventory expectedInventory, bool metaOnly = true, bool checkFileSize = false)
        {
            IEnumerable<GameVersionExtended> missingVersions = expectedInventory.AvailableVersions.OrderBy(v => v.TimeOfUpload)
                .Where(version =>
                {
                    if (version.Type != GameVersion.BuildType.Release)
                        return false;

                    if (!File.Exists(version.GetMetaDataFilePath(Settings.LibraryPath)))
                        return true;

                    if (metaOnly || !Settings.CheckJarFiles)
                        return false;

                    GameVersionExtended expected = new GameVersionExtended(version, File.ReadAllText(version.GetMetaDataFilePath(Settings.LibraryPath)));

                    if (expected.ClientDownloadUrl != null)
                    {
                        if (!File.Exists(version.GetClientFilePath(Settings.LibraryPath)))
                            return true;

                        if (checkFileSize && expected.ClientDownloadSize > 0)
                        {
                            FileInfo fileInfo = new FileInfo(version.GetClientFilePath(Settings.LibraryPath));

                            if (fileInfo.Length != expected.ClientDownloadSize)
                            {
                                fileInfo.Delete();
                                return true;
                            }
                        }
                    }

                    if (expected.ServerDownloadUrl != null)
                    {
                        if (!File.Exists(version.GetServerFilePath(Settings.LibraryPath)))
                            return true;

                        if (checkFileSize && expected.ServerDownloadSize > 0)
                        {
                            FileInfo fileInfo = new FileInfo(version.GetServerFilePath(Settings.LibraryPath));

                            if (fileInfo.Length != expected.ServerDownloadSize)
                            {
                                fileInfo.Delete();
                                return true;
                            }
                        }
                    }

                    return false;
                })
                .Select(v => new GameVersionExtended(v));

            return missingVersions;
        }

        public LauncherInventory GetLatestStoredManifest()
        {
            string manifestFolder = Path.GetDirectoryName(LauncherInventory.GetManifestFilePath(Settings.LibraryPath));

            if (!Directory.Exists(manifestFolder))
                return null;

            string latestManifestFile = Directory.EnumerateFiles(manifestFolder, "*.json").OrderByDescending(Path.GetFileName).FirstOrDefault();

            if (latestManifestFile is null)
                return null;

            return new LauncherInventory(File.ReadAllText(latestManifestFile));
        }

        /// <summary>
        /// Processes all actions based on an update
        /// </summary>
        /// <param name="inventoryUpdate"></param>
        /// <param name="downloadsComplete"></param>
        /// <param name="completedActionIds"></param>
        private void TriggerActions(LauncherInventory.Diff inventoryUpdate, bool downloadsComplete, ref List<int> completedActionIds)
        {
            List<ConditionalAction> actionsToRun = new List<ConditionalAction>(Settings.ConditionalActions);

            bool actionsPerformed;
            do
            {
                actionsPerformed = false;

                for (int i = 0; i < actionsToRun.Count && _running; ++i)
                {
                    if (!actionsToRun[i].ConditionsFulfilled(inventoryUpdate, completedActionIds, downloadsComplete))
                        continue;

                    if (actionsToRun[i].ActionsPerformed(inventoryUpdate, Settings.LibraryPath))
                    {
                        completedActionIds.Add(actionsToRun[i].Id);
                        actionsPerformed = true;
                    }

                    actionsToRun.RemoveAt(i);
                }
            }
            while (actionsPerformed);
        }
    }
}
