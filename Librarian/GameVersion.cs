using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace com.github.Wubbi.Librarian
{
    /// <summary>
    /// Contains basic data about a specific version of game
    /// </summary>
    public class GameVersion : IEquatable<GameVersion>
    {
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


        /// <summary>
        /// Creates a new <see cref="GameVersion"/> object by first parsing the given json snippet from the launchers version manifest
        /// </summary>
        /// <param name="launcherManifestSnippet">The entry in the versions array of the wanted version in the launcher manifest</param>
        public GameVersion(string launcherManifestSnippet)
        {
            JObject manifestSnippet = JObject.Parse(launcherManifestSnippet);

            Id = manifestSnippet["id"].Value<string>();

            switch (manifestSnippet["type"].Value<string>())
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

            VersionMetadataUrl = manifestSnippet["url"].Value<string>();

            TimeOfPublication = manifestSnippet["time"].Value<DateTime>();
            TimeOfUpload = manifestSnippet["releaseTime"].Value<DateTime>();

            LibrarySubFolder = Path.Combine(Type.ToString(), Id, TimeOfUpload.ToUniversalTime().ToString("yyyy-MM-dd_HH-mm-ss_UTC"));
        }

        /// <summary>
        /// Creates a copy of the given <see cref="GameVersion"/>
        /// </summary>
        /// <param name="other">The <see cref="GameVersion"/> to copy</param>
        protected GameVersion(GameVersion other)
        {
            Id = other.Id;
            Type = other.Type;
            VersionMetadataUrl = other.VersionMetadataUrl;
            TimeOfPublication = other.TimeOfPublication;
            TimeOfUpload = other.TimeOfUpload;
            LibrarySubFolder = other.LibrarySubFolder;
        }

        public bool Equals(GameVersion other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && Type == other.Type && string.Equals(VersionMetadataUrl, other.VersionMetadataUrl)
                   && TimeOfPublication.Equals(other.TimeOfPublication) && TimeOfUpload.Equals(other.TimeOfUpload);
        }

        /// <summary>
        /// The full path where this versions metadata should be located in the library
        /// </summary>
        /// <param name="libraryRootFolder">The path to the library</param>
        /// <returns>A full path to the local json file that is supposed to hold this versions metadata</returns>
        public string GetMetaDataFilePath(string libraryRootFolder)
            => Path.Combine(libraryRootFolder, LibrarySubFolder, Id + ".json");

        /// <summary>
        /// The full path where this versions client jar should be located in the library
        /// </summary>
        /// <param name="libraryRootFolder">The path to the library</param>
        /// <returns>A full path to the local client.jar</returns>
        public string GetClientFilePath(string libraryRootFolder)
            => Path.Combine(libraryRootFolder, LibrarySubFolder, "client.jar");

        /// <summary>
        /// The full path where this versions server jar should be located in the library
        /// </summary>
        /// <param name="libraryRootFolder">The path to the library</param>
        /// <returns>A full path to the local server.jar</returns>
        public string GetServerFilePath(string libraryRootFolder)
            => Path.Combine(libraryRootFolder, LibrarySubFolder, "server.jar");

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
                int hashCode = Id != null ? Id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (VersionMetadataUrl != null ? VersionMetadataUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TimeOfPublication.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeOfUpload.GetHashCode();
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Contains more detailed data about a specific version of the game
    /// </summary>
    public class GameVersionExtended : GameVersion
    {
        /// <summary>
        /// The JSON formatted data containing all relevant information for this specific version
        /// </summary>
        public string MetaData { get; }

        /// <summary>
        /// The url from which the client's .jar file can be downloaded or null if no client download is available
        /// </summary>
        public string ClientDownloadUrl { get; }

        /// <summary>
        /// The size of the client's .jar file in bytes
        /// </summary>
        public long ClientDownloadSize { get; }

        /// <summary>
        /// The clients .jar SHA1 hash
        /// </summary>
        public string ClientDownloadSha1 { get; }

        /// <summary>
        /// The url from which the client's .jar file can be downloaded or null if no server download is available
        /// </summary>
        public string ServerDownloadUrl { get; }

        /// <summary>
        /// The size of the server's .jar file in bytes
        /// </summary>
        public long ServerDownloadSize { get; }

        /// <summary>
        /// The servers .jar SHA1 hash
        /// </summary>
        public string ServerDownloadSha1 { get; }

        /// <summary>
        /// Creates a new <see cref="GameVersionExtended"/> using the provided metadata or by downloading the required data according the base <see cref="GameVersion"/>
        /// </summary>
        /// <param name="basis">The manifest based data of this version</param>
        /// <param name="metadata">This versions metadata or null to have it be downloaded according to <paramref name="basis"/></param>
        public GameVersionExtended(GameVersion basis, string metadata = null) : base(basis)
        {
            MetaData = metadata ?? WebAccess.Instance.DownloadFileAsString(VersionMetadataUrl);

            JObject metaData = JObject.Parse(MetaData);

            JToken downloads = metaData["downloads"];

            JToken client = downloads["client"];
            if (client != null)
            {
                ClientDownloadUrl = client["url"].Value<string>();
                ClientDownloadSize = client["size"].Value<long>();
                ClientDownloadSha1 = client["sha1"].Value<string>();

                if (!ClientDownloadUrl.EndsWith(ClientDownloadSha1 + "/client.jar"))
                    Logger.Warning($"Client download for version {Id} has unexpected url");
            }

            JToken server = downloads["server"];
            if (server != null)
            {
                ServerDownloadUrl = server["url"].Value<string>();
                ServerDownloadSize = server["size"].Value<long>();
                ServerDownloadSha1 = server["sha1"].Value<string>();

                if (!ServerDownloadUrl.EndsWith(ServerDownloadSha1 + "/server.jar"))
                    Logger.Warning($"Server download for version {Id} has unexpected url");
            }
        }

        public bool Equals(GameVersionExtended other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && string.Equals(ClientDownloadUrl, other.ClientDownloadUrl) && string.Equals(ServerDownloadUrl, other.ServerDownloadUrl)
                   && ClientDownloadSize == other.ClientDownloadSize && ServerDownloadSize == other.ServerDownloadSize
                   && string.Equals(ClientDownloadSha1, other.ClientDownloadSha1) && string.Equals(ServerDownloadSha1, other.ServerDownloadSha1);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GameVersionExtended)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ClientDownloadUrl != null ? ClientDownloadUrl.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)ClientDownloadSize;
                hashCode = (hashCode * 397) ^ (ClientDownloadSha1 != null ? ClientDownloadSha1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ServerDownloadUrl != null ? ServerDownloadUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)ServerDownloadSize;
                hashCode = (hashCode * 397) ^ (ServerDownloadSha1 != null ? ServerDownloadSha1.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
