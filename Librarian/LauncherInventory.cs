using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace com.github.Wubbi.Librarian
{
    /// <summary>
    /// The data currently accessible through the launcher
    /// </summary>
    public class LauncherInventory : IEquatable<LauncherInventory>
    {
        /// <summary>
        /// The location of the launchers json file to look up the currently available versions
        /// </summary>
        public const string VersionInfoLocation = @"https://launchermeta.mojang.com/mc/game/version_manifest.json";

        /// <summary>
        /// The json data that this <see cref="LauncherInventory"/> was parsed from
        /// </summary>
        public string Manifest { get; }

        /// <summary>
        /// The id of the newest version with type release
        /// </summary>
        public string LatestReleaseId { get; }

        /// <summary>
        /// The id of the newest version with type snapshot
        /// </summary>
        public string LatestSnapshotId { get; }

        /// <summary>
        /// The currently available versions of the game
        /// </summary>
        public ReadOnlyCollection<GameVersion> AvailableVersions { get; }

        /// <summary>
        /// Creates a new <see cref="LauncherInventory"/> based on the given manifest or by downloading and parsing the online manifest
        /// </summary>
        /// <param name="manifestJson">A manifest in json format, or null to download the live one</param>
        public LauncherInventory(string manifestJson = null)
        {
            if (manifestJson == null)
                manifestJson = WebAccess.DownloadFileAsString(VersionInfoLocation);

            Manifest = manifestJson;

            JObject manifest = JObject.Parse(manifestJson);

            LatestReleaseId = manifest["latest"]["release"].ToString();
            LatestSnapshotId = manifest["latest"]["snapshot"].ToString();

            List<GameVersion> gameVersions = manifest["versions"].Select(v => new GameVersion(v.ToString())).ToList();
            AvailableVersions = new ReadOnlyCollection<GameVersion>(gameVersions);
        }

        public static string GetManifestFilePath(string libraryRootFolder)
            => Path.Combine(libraryRootFolder, "Manifests", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd_HH-mm-ss_UTC") + ".json");

        public bool Equals(LauncherInventory other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!string.Equals(LatestReleaseId, other.LatestReleaseId) || !string.Equals(LatestSnapshotId, other.LatestSnapshotId)
                || AvailableVersions.Count != other.AvailableVersions.Count) return false;

            return AvailableVersions.SequenceEqual(other.AvailableVersions);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LauncherInventory)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = LatestReleaseId != null ? LatestReleaseId.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (LatestSnapshotId != null ? LatestSnapshotId.GetHashCode() : 0);

                return AvailableVersions.Aggregate(hashCode, (current, availableVersion) => (current * 397) ^ availableVersion.GetHashCode());
            }
        }


        /// <summary>
        /// Compares two <see cref="LauncherInventory"/> objects and stores the difference between them
        /// </summary>
        public class Diff
        {
            /// <summary>
            /// The id of the newer version with type release, or null if it did not change
            /// </summary>
            public string NewReleaseId { get; }

            /// <summary>
            /// The id of the newer version with type snapshot, or null if it did not change
            /// </summary>
            public string NewSnapshotId { get; }

            /// <summary>
            /// The new versions now available in the launcher
            /// </summary>
            public ReadOnlyCollection<GameVersion> AddedVersions { get; }

            /// <summary>
            /// The versions which existed before, but got changed
            /// </summary>
            public ReadOnlyCollection<GameVersion> ChangedVersions { get; }

            /// <summary>
            /// The versions removed from the launcher
            /// </summary>
            public ReadOnlyCollection<GameVersion> RemovedVersions { get; }

            /// <summary>
            /// The old inventory use for this diff
            /// </summary>
            public LauncherInventory OldInventory { get; }

            /// <summary>
            /// The new inventory used for this diff
            /// </summary>
            public LauncherInventory NewInventory { get; }

            /// <summary>
            /// Creates a new <see cref="Diff"/> based on the changes made from one <see cref="LauncherInventory"/> to another
            /// </summary>
            /// <param name="fromInventory">The old inventory, which got changed</param>
            /// <param name="toInventory">The new inventory, in which the changes are present</param>
            public Diff(LauncherInventory fromInventory, LauncherInventory toInventory)
            {
                OldInventory = fromInventory ?? throw new ArgumentNullException(nameof(fromInventory));
                NewInventory = toInventory ?? throw new ArgumentNullException(nameof(toInventory));

                if (fromInventory.LatestReleaseId != toInventory.LatestReleaseId)
                    NewReleaseId = toInventory.LatestReleaseId;

                if (fromInventory.LatestSnapshotId != toInventory.LatestSnapshotId)
                    NewSnapshotId = toInventory.LatestSnapshotId;


                IEnumerable<string> oldIds = fromInventory.AvailableVersions.Select(v => v.Id);
                IEnumerable<string> newIds = toInventory.AvailableVersions.Select(v => v.Id);

                IEnumerable<string> sharedIds = oldIds.Intersect(newIds);

                RemovedVersions = new ReadOnlyCollection<GameVersion>(fromInventory.AvailableVersions.Where(v => !sharedIds.Contains(v.Id)).ToList());
                AddedVersions = new ReadOnlyCollection<GameVersion>(toInventory.AvailableVersions.Where(v => !sharedIds.Contains(v.Id)).ToList());
                ChangedVersions = new ReadOnlyCollection<GameVersion>(toInventory.AvailableVersions.Except(fromInventory.AvailableVersions).Where(v => sharedIds.Contains(v.Id)).ToList());
            }
        }
    }
}
