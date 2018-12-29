using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Librarian
{
    /// <summary>
    /// Contains all known data about a specific version of game
    /// </summary>
    public class GameVersion : IEquatable<GameVersion>
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
        /// The date and time when this version was made available to the launcher
        /// </summary>
        public DateTime TimeOfPublication { get; }

        /// <summary>
        /// The date and time when this version was uploaded
        /// </summary>
        public DateTime TimeOfUpload { get; }

        /// <summary>
        /// The folder in which this version is stored within the library main folder
        /// </summary>
        public string LibrarySubFolder { get; }


        //### Secondary data from the versions direct metadata ###

        /// <summary>
        /// Whether or not the specific metadata for this version was loaded and parsed
        /// </summary>
        public bool MetadataWasLoaded { get; }

        /// <summary>
        /// The url from which the client's .jar file can be downloaded or null if no client download is available
        /// </summary>
        public string ClientDownloadUrl { get; }

        /// <summary>
        /// The size of the client's .jar file in bytes
        /// </summary>
        public long ClientDownloadSize { get; }

        /// <summary>
        /// The url from which the client's .jar file can be downloaded or null if no server download is available
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
        /// <param name="parseOnly">If set to true no metadata will be downloaded and only the given string is parsed</param>
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
                    Type = BuildType.Alpha;
                    break;
                case "old_beta":
                    Type = BuildType.Beta;
                    break;
            }

            VersionMetadataUrl = manifestSnippet["url"].ToString();

            TimeOfPublication = DateTime.Parse(manifestSnippet["time"].ToString());
            TimeOfUpload = DateTime.Parse(manifestSnippet["releaseTime"].ToString());

            LibrarySubFolder = Path.Combine(Type.ToString(), Id, TimeOfUpload.ToUniversalTime().ToString("yyyy-MM-dd_HH-mm-ss"));

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

            MetadataWasLoaded = true;
        }

        /// <summary>
        /// Creates a new instance of <see cref="GameVersion"/> with the primary values copied from <paramref name="other"/>, but with a fresh download of the metadata
        /// </summary>
        /// <param name="other">The <see cref="GameVersion"/> to copy primary data from</param>
        public GameVersion(GameVersion other)
        {
            Id = other.Id;
            Type = other.Type;
            VersionMetadataUrl = other.VersionMetadataUrl;
            TimeOfPublication = other.TimeOfPublication;
            TimeOfUpload = other.TimeOfUpload;
            LibrarySubFolder = other.LibrarySubFolder;

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

            MetadataWasLoaded = true;
        }

        /// <summary>
        /// The build type (release or snapshot)
        /// </summary>
        public enum BuildType
        {
            Unknown = 0,
            Release = 1,
            Snapshot = 2,
            Alpha = 3,
            Beta = 4
        }

        public bool Equals(GameVersion other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && Type == other.Type && string.Equals(VersionMetadataUrl, other.VersionMetadataUrl)
                   && TimeOfPublication.Equals(other.TimeOfPublication) && TimeOfUpload.Equals(other.TimeOfUpload)
                   && MetadataWasLoaded == other.MetadataWasLoaded
                   && string.Equals(ClientDownloadUrl, other.ClientDownloadUrl) && ClientDownloadSize == other.ClientDownloadSize
                   && string.Equals(ServerDownloadUrl, other.ServerDownloadUrl) && ServerDownloadSize == other.ServerDownloadSize;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GameVersion)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (VersionMetadataUrl != null ? VersionMetadataUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TimeOfPublication.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeOfUpload.GetHashCode();
                hashCode = (hashCode * 397) ^ MetadataWasLoaded.GetHashCode();
                hashCode = (hashCode * 397) ^ (ClientDownloadUrl != null ? ClientDownloadUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ClientDownloadSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (ServerDownloadUrl != null ? ServerDownloadUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ServerDownloadSize.GetHashCode();
                return hashCode;
            }
        }
    }
}
