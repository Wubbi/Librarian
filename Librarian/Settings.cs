using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace com.github.Wubbi.Librarian
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
        /// If true, Librarian will trigger Actions for any missed updates since the last run before waiting for updates of the live manifest
        /// </summary>
        public bool ProcessMissedUpdates { get; }

        /// <summary>
        /// If true, .jar files will be checked as part of the library update as well, not just the existence of metadata.
        /// Also determines if a fresh Library will be filled with .jar files and if <see cref="MaintainInventory"/> metadata files only
        /// </summary>
        public bool CheckJarFiles { get; }

        /// <summary>
        /// If true, the current newest stored manifest will be compared the existing files in the library when Librarian starts.
        /// Missing files then are downloaded and added. Does not validate the found files, only matches their names
        /// </summary>
        public bool MaintainInventory { get; }

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
            CheckJarFiles = settings["checkJarFiles"]?.Value<bool>() ?? false;
            MaintainInventory = settings["maintainInventory"]?.Value<bool>() ?? false;

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
