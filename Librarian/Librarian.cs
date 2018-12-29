﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Librarian
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
        /// A list af actions that execute if their filters match the current circumstances
        /// </summary>
        private readonly ReadOnlyCollection<ConditionalAction> _conditionalActions;

        /// <summary>
        /// A list of subsequent updates on the launcher manifest
        /// </summary>
        private readonly BlockingCollection<LauncherInventory.Diff> _launcherManifestUpdates;

        public Librarian()
        {
            _launcherManifestUpdates = new BlockingCollection<LauncherInventory.Diff>();

            _manifestWatcher = new ManifestWatcher();

            _manifestWatcher.ChangeInLauncherManifest += diff => _launcherManifestUpdates.Add(diff);
        }

        /// <summary>
        /// This method blocks the current thread in between updates of the launcher manifest. Will perform actions matching a filter for each update.
        /// </summary>
        public void Run()
        {
            _manifestWatcher.Start(TimeSpan.FromSeconds(Settings.ManifestRefreshRate));

            foreach (LauncherInventory.Diff update in _launcherManifestUpdates)
            {
                TriggerActions(update, false);
                MaintainLibrary(update);
                TriggerActions(update, true);
            }
        }

        private void MaintainLibrary(LauncherInventory.Diff inventoryUpdate)
        {
            if (!Directory.Exists(Settings.LibraryPath))
                Directory.CreateDirectory(Settings.LibraryPath);

            IEnumerable<GameVersion> versionsToCheck = inventoryUpdate.AddedVersions.Concat(inventoryUpdate.ChangedVersions).OrderBy(v => v.TimeOfUpload);

            foreach (GameVersion gameVersion in versionsToCheck)
            {
                string intendedPath = Path.Combine(Settings.LibraryPath, gameVersion.LibrarySubFolder);

                if (Directory.Exists(intendedPath))
                    continue;

                GameVersion withMetadata = new GameVersion(gameVersion);

                if (withMetadata.ServerDownloadUrl != null)
                {
                    string path = Path.Combine(intendedPath, "Server");

                    Directory.CreateDirectory(path);

                    WebAccess.DownloadAndStoreFile(withMetadata.ServerDownloadUrl, path, withMetadata.ServerDownloadSize);
                }

                if (withMetadata.ClientDownloadUrl != null)
                {
                    string path = Path.Combine(intendedPath, "Client");

                    Directory.CreateDirectory(path);

                    WebAccess.DownloadAndStoreFile(withMetadata.ClientDownloadUrl, path, withMetadata.ClientDownloadSize);
                }
            }
        }

        /// <summary>
        /// Processes all actions based on an update
        /// </summary>
        /// <param name="inventoryUpdate"></param>
        /// <param name="downloadsComplete"></param>
        private void TriggerActions(LauncherInventory.Diff inventoryUpdate, bool downloadsComplete)
        {
            List<ConditionalAction> actionsToRun = new List<ConditionalAction>(_conditionalActions);
            List<int> completedIds = new List<int>();

            bool actionsPerformed;
            do
            {
                actionsPerformed = false;

                for (int i = 0; i < actionsToRun.Count; ++i)
                {
                    if (!actionsToRun[i].ConditionsFulfilled(inventoryUpdate, completedIds, downloadsComplete))
                        continue;

                    if (actionsToRun[i].ActionsPerformed(inventoryUpdate))
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
            _manifestWatcher?.Dispose();
            _launcherManifestUpdates?.Dispose();
        }
    }
}
