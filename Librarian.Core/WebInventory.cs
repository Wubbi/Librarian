using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Librarian.Core
{
    public class WebInventory
    {
        public const string LauncherManifest = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

        public static readonly WebInventory Empty;

        static WebInventory()
        {
            Empty = new WebInventory();
        }

        public string Json { get; }

        public ReadOnlyDictionary<Game.BuildType, string> Latest { get; }

        public ReadOnlyCollection<Game> Versions { get; }

        private WebInventory()
        {
            Json = "";
            Latest = new ReadOnlyDictionary<Game.BuildType, string>(new Dictionary<Game.BuildType, string>());
            Versions = new ReadOnlyCollection<Game>(new List<Game>());
        }

        public WebInventory([NotNull] string json)
        {
            Json = json ?? throw new ArgumentNullException(nameof(json));

            using JsonDocument jsonDoc = JsonDocument.Parse(json);

            Dictionary<Game.BuildType, string> latest = new Dictionary<Game.BuildType, string>();

            foreach (JsonProperty property in jsonDoc.RootElement.GetProperty("latest").EnumerateObject())
            {
                if (!Game.TryParseBuildType(property.Name, out Game.BuildType buildType))
                    continue;

                latest[buildType] = property.Value.GetString();
            }

            Latest = new ReadOnlyDictionary<Game.BuildType, string>(latest);

            List<Game> versions = new List<Game>();

            foreach (JsonElement element in jsonDoc.RootElement.GetProperty("versions").EnumerateArray())
            {
                versions.Add(new Game(element.GetRawText()));
            }

            Versions = new ReadOnlyCollection<Game>(versions);
        }
    }
}
