using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Librarian
{
    /// <summary>
    /// Stores all the settings made with the settings file
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The time interval in seconds at which the launcher manifest file is reread to see if any updates were made
        /// </summary>
        public int ManifestRefreshRate { get; }

        /// <summary>
        /// The path to the directory in which the library is to be maintained
        /// </summary>
        public string LibraryPath { get; }

        /// <summary>
        /// If true, the librarian will download missing any versions (no filters apply) based on the live manifest before updates are tracked
        /// </summary>
        public bool ProcessMissedUpdates { get; }

        /// <summary>
        /// A list af actions that execute if their filters match the current circumstances
        /// </summary>
        public ReadOnlyCollection<ConditionalAction> ConditionalActions { get; }

        public Settings(string json)
        {
            JObject settings = JObject.Parse(json);

            ManifestRefreshRate = settings["refreshRate"]?.Value<int>() ?? 29;
            LibraryPath = settings["libraryPath"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(LibraryPath))
                LibraryPath = Path.Combine(Environment.CurrentDirectory, "Library");

            ProcessMissedUpdates = settings["addMissingVersions"]?.Value<bool>() ?? false;

            JToken tasks = settings["tasks"];
            if (tasks is null)
            {
                ConditionalActions = new ReadOnlyCollection<ConditionalAction>(new List<ConditionalAction>());
                return;
            }

            ConditionalActions = new ReadOnlyCollection<ConditionalAction>(tasks.Children().Select((t, i) => new ConditionalAction(i, t.ToString())).ToList());
        }
    }
}
