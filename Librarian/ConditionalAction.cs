using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Librarian
{
    /// <summary>
    /// Contains conditions for when this action is supposed to trigger as well as the action itself
    /// </summary>
    public class ConditionalAction
    {
        /// <summary>
        /// The unique identifier for this <see cref="ConditionalAction"/>
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// If True, triggers before, if False, after the downloads ar made
        /// </summary>
        private readonly bool _beforeDownload;

        /// <summary>
        /// Actions that need to be completed successfully 
        /// </summary>
        private readonly ReadOnlyCollection<int> _dependentOnIds;

        /// <summary>
        /// The kind of release this Action targets
        /// </summary>
        private readonly GameVersion.BuildType _type;

        /// <summary>
        /// Only trigger for the latest version, ignore other changes
        /// </summary>
        private readonly bool _onLatest;

        /// <summary>
        /// Trigger for newly added versions. Ignored if <see cref="_onLatest"/> is True
        /// </summary>
        private readonly bool _onAdded;
        /// <summary>
        /// Trigger for changed versions. Ignored if <see cref="_onLatest"/> is True
        /// </summary>
        private readonly bool _onChanged;
        /// <summary>
        /// Trigger for removed versions. Ignored if <see cref="_onLatest"/> is True
        /// </summary>
        private readonly bool _onRemoved;

        /// <summary>
        /// The commands to execute if the conditions apply
        /// </summary>
        private readonly ReadOnlyCollection<string> _commands;

        /// <summary>
        /// Replace pattern for the version Id
        /// </summary>
        private readonly string _paramId;

        /// <summary>
        /// Replace pattern for the download folder
        /// </summary>
        private readonly string _paramPath;

        /// <summary>
        /// Creates a new <see cref="ConditionalAction"/> based on the settings given as json string
        /// </summary>
        /// <param name="id"></param>
        /// <param name="json"></param>
        public ConditionalAction(int id, string json)
        {
            Id = id;

            JObject settings = JObject.Parse(json);

            _beforeDownload = settings["beforeDownload"]?.Value<bool>() ?? false;
            _dependentOnIds = new ReadOnlyCollection<int>(settings["dependentOnIds"]?.Values<int>()?.ToList() ?? new List<int>());
            if (!Enum.TryParse(settings["type"]?.Value<string>(), true, out _type))
                _type = GameVersion.BuildType.Release;
            _onLatest = settings["onLatest"]?.Value<bool>() ?? true;

            if (!_onLatest)
            {
                _onAdded = settings["onAdded"]?.Value<bool>() ?? true;
                _onChanged = settings["onChanged"]?.Value<bool>() ?? false;
                _onRemoved = settings["onRemoved"]?.Value<bool>() ?? false;
            }

            _commands = new ReadOnlyCollection<string>(settings["commands"]?.Values<string>()?.ToList() ?? new List<string>());

            JToken parameter = settings["params"];
            if (parameter is null)
                return;

            _paramId = parameter["id"]?.Value<string>();
            _paramPath = parameter["path"]?.Value<string>();
        }

        /// <summary>
        /// Checks if the filters and conditions match the current update
        /// </summary>
        /// <param name="inventoryUpdate"></param>
        /// <param name="completedIds"></param>
        /// <param name="downloadsCompleted"></param>
        /// <returns></returns>
        public bool ConditionsFulfilled(LauncherInventory.Diff inventoryUpdate, IEnumerable<int> completedIds, bool downloadsCompleted)
        {
            if (downloadsCompleted == _beforeDownload)
                return false;

            if (_dependentOnIds.Except(completedIds).Any())
                return false;

            if (_onLatest)
            {
                if (_type == GameVersion.BuildType.Release && inventoryUpdate.NewReleaseId == null)
                    return false;

                if (_type == GameVersion.BuildType.Snapshot && inventoryUpdate.NewSnapshotId == null)
                    return false;

                return true;
            }

            if (_onAdded && inventoryUpdate.AddedVersions.Any(v => v.Type == _type))
                return true;

            if (_onChanged && inventoryUpdate.ChangedVersions.Any(v => v.Type == _type))
                return true;

            if (_onRemoved && inventoryUpdate.RemovedVersions.Any(v => v.Type == _type))
                return true;

            return false;
        }

        /// <summary>
        /// Performs the stored commands on all versions of the game that have had an update of some kind and match the filter
        /// </summary>
        /// <param name="inventoryUpdate"></param>
        /// <param name="libraryRootPath"></param>
        /// <returns>True if all commands executed without errors for all eligible versions, False otherwise</returns>
        public bool ActionsPerformed(LauncherInventory.Diff inventoryUpdate, string libraryRootPath)
        {
            List<GameVersion> versionsToProcess = new List<GameVersion>();
            if (_onLatest)
            {
                if (_type == GameVersion.BuildType.Snapshot && inventoryUpdate.NewSnapshotId != null)
                    versionsToProcess.Add(inventoryUpdate.NewInventory.AvailableVersions.First(v => v.Id == inventoryUpdate.NewSnapshotId));
                else if (_type == GameVersion.BuildType.Release && inventoryUpdate.NewReleaseId != null)
                    versionsToProcess.Add(inventoryUpdate.NewInventory.AvailableVersions.First(v => v.Id == inventoryUpdate.NewReleaseId));
            }
            else
            {
                if (_onAdded)
                    versionsToProcess.AddRange(inventoryUpdate.AddedVersions.Where(v => v.Type == _type));

                if (_onChanged)
                    versionsToProcess.AddRange(inventoryUpdate.ChangedVersions.Where(v => v.Type == _type));

                if (_onRemoved)
                    versionsToProcess.AddRange(inventoryUpdate.RemovedVersions.Where(v => v.Type == _type));
            }

            bool allSucceeded = true;
            foreach (GameVersion gameVersion in versionsToProcess.OrderBy(v => v.TimeOfUpload))
                if (!RunCommandsOnVersion(gameVersion, libraryRootPath))
                    allSucceeded = false;

            return allSucceeded;
        }

        /// <summary>
        /// Executes all stored commands based on a specific version of the game
        /// </summary>
        /// <param name="version"></param>
        /// <param name="libraryRootPath"></param>
        /// <returns>True if all commands executed without errors, False otherwise</returns>
        private bool RunCommandsOnVersion(GameVersion version, string libraryRootPath)
        {
            bool allCommandsSuccessful = true;

            foreach (string originalCommand in _commands)
            {
                string command = originalCommand;

                if (_paramId != null)
                    command = command.Replace(_paramId, version.Id);

                if (_paramPath != null)
                    command = command.Replace(_paramPath, Path.Combine(libraryRootPath, version.LibrarySubFolder));

                Logger.Info("Executing command: " + command);

                int exitCode = CommandExecution.RunWaitCommand(command);

                if (exitCode >= 0)
                    continue;

                Logger.Error("Exit code " + exitCode);
                allCommandsSuccessful = false;
            }

            return allCommandsSuccessful;
        }
    }
}
