using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Librarian
{
    /// <summary>
    /// The data currently accessible through the launcher
    /// </summary>
    public class LauncherInventory
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
        public IReadOnlyList<GameVersion> AvailableVersions { get; }

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
    }
}
