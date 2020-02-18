using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Librarian.Core
{
    public class Game : IEquatable<Game>
    {
        /// <summary>
        /// The build type
        /// </summary>
        public enum BuildType
        {
            Unknown = 0,
            Release = 1,
            Snapshot = 2,
            Alpha = 3,
            Beta = 4
        }

        public enum AppType
        {
            Unknown = 0,
            Client = 1,
            Server = 2
        }

        /// <summary>
        /// An empty instance of <see cref="Game"/> with default values
        /// </summary>
        public static readonly Game None;

        static Game()
        {
            None = new Game();
        }

        /// <summary>
        /// The json data this version was parsed from
        /// </summary>
        public string Json { get; }

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
        public string Url { get; }

        public DateTime ReleaseTime { get; }

        public DateTime Time { get; }

        public ReadOnlyDictionary<AppType, DownloadMeta> Downloads { get; }

        private Game()
        {
            Json = "";
            Id = "";
            Type = BuildType.Unknown;
            Url = "";
            ReleaseTime = DateTime.MinValue;
            Time = DateTime.MinValue;
            Downloads = new ReadOnlyDictionary<AppType, DownloadMeta>(new Dictionary<AppType, DownloadMeta>());
        }

        public Game([NotNull] string json) : this()
        {
            Json = json ?? throw new ArgumentNullException(nameof(json));

            using JsonDocument jsonDoc = JsonDocument.Parse(json);

            Id = jsonDoc.RootElement.GetProperty("id").GetString();

            Url = jsonDoc.RootElement.TryGetProperty("url", out JsonElement url) ? url.GetString() : "";

            TryParseBuildType(jsonDoc.RootElement.GetProperty("type").GetString(), out BuildType buildType);
            Type = buildType;

            ReleaseTime = jsonDoc.RootElement.GetProperty("releaseTime").GetDateTime();

            Time = jsonDoc.RootElement.GetProperty("time").GetDateTime();

            Dictionary<AppType, DownloadMeta> downloadList = new Dictionary<AppType, DownloadMeta>();

            if (jsonDoc.RootElement.TryGetProperty("downloads", out JsonElement downloads))
            {
                foreach (JsonProperty download in downloads.EnumerateObject())
                {
                    if (!Enum.TryParse(download.Name, true, out AppType appType))
                        continue;

                    downloadList[appType] = new DownloadMeta(
                        download.Value.GetProperty("sha1").GetString(),
                        download.Value.GetProperty("size").GetInt64(),
                        download.Value.GetProperty("url").GetString()
                    );
                }
            }

            Downloads = new ReadOnlyDictionary<AppType, DownloadMeta>(downloadList);
        }

        public bool Equals([NotNull] Game other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (!Id.Equals(other.Id))
                return false;

            if (!Type.Equals(other.Type))
                return false;

            if (!ReleaseTime.Equals(other.ReleaseTime))
                return false;

            return Time.Equals(other.Time);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Type;
                hashCode = (hashCode * 397) + Id.GetHashCode(StringComparison.InvariantCulture);
                hashCode = (hashCode * 397) + ReleaseTime.GetHashCode();
                hashCode = (hashCode * 397) + Time.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{BuildTypeToString(Type)} {Id}";
        }

        public static string BuildTypeToString(BuildType type)
        {
            return type switch
            {
                BuildType.Release => "Release",
                BuildType.Snapshot => "Snapshot",
                BuildType.Alpha => "Alpha",
                BuildType.Beta => "Beta",
                BuildType.Unknown => "Unknown",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static bool TryParseBuildType(string value, out BuildType type)
        {
            switch (value.ToLowerInvariant())
            {
                case "release":
                    type = BuildType.Release;
                    return true;
                case "snapshot":
                    type = BuildType.Snapshot;
                    return true;
                case "old_alpha":
                    type = BuildType.Alpha;
                    return true;
                case "old_beta":
                    type = BuildType.Beta;
                    return true;
            }

            type = BuildType.Unknown;
            return false;
        }

        public class DownloadMeta : IEquatable<DownloadMeta>
        {
            public string Sha1 { get; }

            public long Size { get; }

            public string Url { get; }

            internal DownloadMeta(string sha1, long size, string url)
            {
                Sha1 = sha1;
                Size = size;
                Url = url;
            }

            public bool Equals(DownloadMeta other)
            {
                if (ReferenceEquals(other, null))
                    return false;

                if (ReferenceEquals(this, other))
                    return true;

                if (!Size.Equals(other.Size))
                    return false;

                if (!Sha1.Equals(other.Sha1))
                    return false;

                return Url.Equals(other.Url);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Size.GetHashCode();
                    hashCode = (hashCode * 397) + Url.GetHashCode(StringComparison.InvariantCulture);
                    hashCode = (hashCode * 397) + Sha1.GetHashCode(StringComparison.InvariantCulture);
                    return hashCode;
                }
            }
        }
    }
}
