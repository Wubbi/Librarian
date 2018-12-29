﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Librarian
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
        /// Creates a new <see cref="LauncherInventory"/> by reading and parsing the current launcher version manifest
        /// </summary>
        public LauncherInventory()
        {
            string rawJson = WebAccess.DownloadFileAsString(VersionInfoLocation);

            JObject manifest = JObject.Parse(rawJson);

            LatestReleaseId = manifest["latest"]["release"].ToString();
            LatestSnapshotId = manifest["latest"]["snapshot"].ToString();

            List<GameVersion> gameVersions = manifest["versions"].Select(v => new GameVersion(v.ToString(), true)).ToList();
            AvailableVersions = new ReadOnlyCollection<GameVersion>(gameVersions);
        }

        public bool Equals(LauncherInventory other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!string.Equals(LatestReleaseId, other.LatestReleaseId) || !string.Equals(LatestSnapshotId, other.LatestSnapshotId) || AvailableVersions.Count != other.AvailableVersions.Count)
                return false;

            return AvailableVersions.SequenceEqual(other.AvailableVersions);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LauncherInventory)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (LatestReleaseId != null ? LatestReleaseId.GetHashCode() : 0);
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
            /// Creates a new <see cref="Diff"/> based on the changes made from one <see cref="LauncherInventory"/> to another
            /// </summary>
            /// <param name="fromInventory">The old inventory, which got changed</param>
            /// <param name="toInventory">The new inventory, in which the changes are present</param>
            public Diff(LauncherInventory fromInventory, LauncherInventory toInventory)
            {
                if (fromInventory is null)
                    throw new ArgumentNullException(nameof(fromInventory));

                if (toInventory is null)
                    throw new ArgumentNullException(nameof(toInventory));

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
