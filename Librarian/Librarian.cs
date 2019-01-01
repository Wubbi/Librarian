using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace com.github.Wubbi.Librarian
{
    /// <summary>
    /// Triggers events and maintains the library
    /// </summary>
    public class Librarian : IDisposable
    {
        /// <summary>
        /// The settings this <see cref="Librarian"/> was initialized with
        /// </summary>
        public Settings Settings { get; }

        /// <summary>
        /// The watcher that fires an event on every change of the launchers manifest
        /// </summary>
        private readonly ManifestWatcher _manifestWatcher;

        /// <summary>
        /// A list of subsequent updates on the launcher manifest
        /// </summary>
        private readonly BlockingCollection<LauncherInventory.Diff> _launcherManifestUpdates;

        public Librarian(string settingsFile)
        {
            if (!File.Exists(settingsFile))
                throw new ArgumentException($"Can't find settings \"{settingsFile}\"", nameof(settingsFile));

            Settings = new Settings(File.ReadAllText(settingsFile));

            if (!Directory.Exists(Settings.LibraryPath))
                Directory.CreateDirectory(Settings.LibraryPath);

            Logger.SetLogFilePath(Path.Combine(Settings.LibraryPath, "log.txt"));

            _launcherManifestUpdates = new BlockingCollection<LauncherInventory.Diff>();

            _manifestWatcher = new ManifestWatcher(null);

            _manifestWatcher.ChangeInLauncherManifest += diff => _launcherManifestUpdates.Add(diff);

            Logger.Info($"Librarian {Assembly.GetExecutingAssembly().GetName().Version} initialized");
        }

        /// <summary>
        /// This method blocks the current thread in between updates of the launcher manifest. Will perform actions matching a filter for each update.
        /// </summary>
        public void Run()
        {
            Logger.Info("Starting run");

            if (Settings.ProcessMissedUpdates)
            {
                Logger.Info("Processing missing versions");
                UpdateLibrary(_manifestWatcher.CurrentInventory);
            }

            _manifestWatcher.Start(TimeSpan.FromSeconds(Settings.ManifestRefreshRate));

            foreach (LauncherInventory.Diff update in _launcherManifestUpdates.GetConsumingEnumerable())
            {
                Logger.Info("Processing update of manifest");
                TriggerActions(update, false);
                UpdateLibrary(update.NewInventory);
                TriggerActions(update, true);
                Logger.Info("Update of manifest processed");
            }
        }

        /// <summary>
        /// Updates the Library and returns a list of versions that got added
        /// </summary>
        /// <param name="launcherInventory">The inventory the library should update to, or null to reference the latest stored inventory</param>
        /// <returns></returns>
        private List<GameVersionExtended> UpdateLibrary(LauncherInventory launcherInventory = null)
        {
            Logger.Info("Updating library");

            string manifestFolder = Path.Combine(Settings.LibraryPath, "Manifests");

            LauncherInventory latestStoredManifest = null;
            if (Directory.Exists(manifestFolder))
            {
                string latestManifestFile = Directory.EnumerateFiles(manifestFolder, "*.json").OrderByDescending(Path.GetFileName).FirstOrDefault();

                if (latestManifestFile != null)
                    latestStoredManifest = new LauncherInventory(File.ReadAllText(latestManifestFile));
            }

            if (launcherInventory == null && latestStoredManifest == null)
            {
                Logger.Info("No manifest found to update");
                return new List<GameVersionExtended>();
            }

            if (launcherInventory == null)
            {
                Logger.Info("Loading latest stored manifest");
                launcherInventory = latestStoredManifest;
            }
            else if (!launcherInventory.Equals(latestStoredManifest))
            {
                if (!Directory.Exists(manifestFolder))
                    Directory.CreateDirectory(manifestFolder);

                string manifestName = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd_HH-mm-ss_UTC") + ".json";

                File.WriteAllText(Path.Combine(manifestFolder, manifestName), launcherInventory.Manifest);
                Logger.Info("Added new manifest");
            }

            IEnumerable<GameVersionExtended> missingVersions = launcherInventory.AvailableVersions.OrderBy(v => v.TimeOfUpload)
                .Where(v => !File.Exists(Path.Combine(Settings.LibraryPath, v.LibrarySubFolder, v.Id + ".json")))
                .Select(v => new GameVersionExtended(v));

            List<GameVersionExtended> addedVersions = new List<GameVersionExtended>();

            foreach (GameVersionExtended version in missingVersions)
            {
                Logger.Info($"Updating {version.Type} {version.Id}");

                string versionRootPath = Path.Combine(Settings.LibraryPath, version.LibrarySubFolder);

                if (!Directory.Exists(versionRootPath))
                    Directory.CreateDirectory(versionRootPath);

                File.WriteAllText(Path.Combine(versionRootPath, version.Id + ".json"), version.MetaData);

                if (version.ServerDownloadUrl != null)
                {
                    string path = Path.Combine(versionRootPath, "server.jar");
                    try
                    {
                        Logger.Info($"Downloading server.jar ({version.ServerDownloadSize / 1024.0 / 1024.0:F2} MB)");
                        WebAccess.DownloadAndStoreFile(version.ServerDownloadUrl, path, version.ServerDownloadSize, version.ServerDownloadSha1);
                        Logger.Info("Download of server.jar complete");
                    }
                    catch (Exception e)
                    {
                        e.Data["File"] = path;
                        Logger.Error("Error while downloading server.jar");
                        Logger.Exception(e);
                    }
                }

                if (version.ClientDownloadUrl != null)
                {
                    string path = Path.Combine(versionRootPath, "client.jar");
                    try
                    {
                        Logger.Info($"Downloading client.jar ({version.ClientDownloadSize / 1024.0 / 1024.0:F2} MB)");
                        WebAccess.DownloadAndStoreFile(version.ClientDownloadUrl, path, version.ClientDownloadSize, version.ClientDownloadSha1);
                        Logger.Info("Download of client.jar complete");
                    }
                    catch (Exception e)
                    {
                        e.Data["File"] = path;
                        Logger.Error("Error while downloading client.jar");
                        Logger.Exception(e);
                    }
                }

                addedVersions.Add(version);

                Logger.Info($"Update of {version.Type} {version.Id} complete");
            }

            switch (addedVersions.Count)
            {
                case 0:
                    Logger.Info("No updates were required");
                    break;
                case 1:
                    Logger.Info("Added 1 missing version");
                    break;
                default:
                    Logger.Info($"Added {addedVersions.Count} missing versions");
                    break;
            }

            Logger.Info("Library is up to date");

            return addedVersions;
        }

        /// <summary>
        /// Processes all actions based on an update
        /// </summary>
        /// <param name="inventoryUpdate"></param>
        /// <param name="downloadsComplete"></param>
        private void TriggerActions(LauncherInventory.Diff inventoryUpdate, bool downloadsComplete)
        {
            List<ConditionalAction> actionsToRun = new List<ConditionalAction>(Settings.ConditionalActions);
            List<int> completedIds = new List<int>();

            bool actionsPerformed;
            do
            {
                actionsPerformed = false;

                for (int i = 0; i < actionsToRun.Count; ++i)
                {
                    if (!actionsToRun[i].ConditionsFulfilled(inventoryUpdate, completedIds, downloadsComplete))
                        continue;

                    if (actionsToRun[i].ActionsPerformed(inventoryUpdate, Settings.LibraryPath))
                    {
                        completedIds.Add(actionsToRun[i].Id);
                        actionsPerformed = true;
                    }

                    actionsToRun.RemoveAt(i);
                }
            }
            while (actionsPerformed);
        }

        public void Dispose()
        {
            Logger.Info("Disposing Librarian");
            _manifestWatcher?.Dispose();
            _launcherManifestUpdates?.Dispose();
        }
    }
}
