using System;
using Newtonsoft.Json.Linq;

namespace Librarian
{
    /// <summary>
    /// Contains all known data about a specific version of game
    /// </summary>
    public class GameVersion
    {
        //### Primary data from version manifest ###

        /// <summary>
        /// The "name" of this version
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The type of release this version represents
        /// </summary>
        public BuildType Type { get; }

        /// <summary>
        /// The url to the detailed metadata for this version
        /// </summary>
        public string VersionMetadataUrl { get; }

        /// <summary>
        /// The date and time when this version was uploaded
        /// </summary>
        public DateTime TimeOfUpload { get; }

        /// <summary>
        /// The date and time when this version was made available to the launcher
        /// </summary>
        public DateTime TimeOfPublication { get; }


        //### Secondary data from the versions direct metadata ###

        /// <summary>
        /// The url from which the client's .jar file can be downloaded
        /// </summary>
        public string ClientDownloadUrl { get; }

        /// <summary>
        /// The size of the client's .jar file in bytes
        /// </summary>
        public long ClientDownloadSize { get; }

        /// <summary>
        /// The url from which the client's .jar file can be downloaded
        /// </summary>
        public string ServerDownloadUrl { get; }

        /// <summary>
        /// The size of the server's .jar file in bytes
        /// </summary>
        public long ServerDownloadSize { get; }


        /// <summary>
        /// Creates a new <see cref="GameVersion"/> object by first parsing the given json snippet from the launchers version manifest
        /// followed by downloading the appropriate metadata for this version
        /// </summary>
        /// <param name="launcherJson">The entry in the versions array of the wanted version in the launcher manifest</param>
        /// <param name="parseOnly">If set to true no additional data will be downloaded and only the given values are parsed</param>
        public GameVersion(string launcherJson, bool parseOnly = false)
        {
            JObject manifestSnippet = JObject.Parse(launcherJson);

            Id = manifestSnippet["id"].ToString();

            switch (manifestSnippet["type"].ToString())
            {
                case "release":
                    Type = BuildType.Release;
                    break;
                case "snapshot":
                    Type = BuildType.Snapshot;
                    break;
                case "old_alpha":
                    Type = BuildType.LegacyAlpha;
                    break;
                case "old_beta":
                    Type = BuildType.LegacyBeta;
                    break;
            }

            VersionMetadataUrl = manifestSnippet["url"].ToString();

            TimeOfPublication = DateTime.Parse(manifestSnippet["time"].ToString());
            TimeOfUpload = DateTime.Parse(manifestSnippet["releaseTime"].ToString());

            if (parseOnly)
                return;

            JObject metadata = JObject.Parse(WebAccess.DownloadFileAsString(VersionMetadataUrl));

            JToken downloads = metadata["downloads"];

            JToken client = downloads["client"];
            if (client != null)
            {
                ClientDownloadUrl = client["url"].ToString();
                if (long.TryParse(client["size"].ToString(), out long size))
                    ClientDownloadSize = size;
            }

            JToken server = downloads["server"];
            if (server != null)
            {
                ServerDownloadUrl = server["url"].ToString();
                if (long.TryParse(server["size"].ToString(), out long size))
                    ServerDownloadSize = size;
            }
        }

        /// <summary>
        /// The build type (release or snapshot)
        /// </summary>
        public enum BuildType
        {
            Unknown = 0,
            Release = 1,
            Snapshot = 2,
            LegacyAlpha = 3,
            LegacyBeta = 4
        }
    }
}
