using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Librarian.Core
{
    public class Settings
    {
        private const string DefaultSettingsFileName = "settings.json";

        public static readonly Settings Default;

        static Settings()
        {
            string file = Path.Combine(Environment.CurrentDirectory, DefaultSettingsFileName);

            if (File.Exists(file))
                Default = new Settings(File.ReadAllText(file));
            else
                Default = new Settings();
        }

        private List<Game.AppType> _ignoredAppTypes;

        public string LibraryRoot { get; private set; }

        public bool Log { get; private set; }

        public bool ValidateLibraryOnStartup { get; private set; }

        public int Interval { get; private set; }

        public bool SkipJars { get; private set; }

        public string MigrateOldLibrarianData { get; private set; }

        public string AddedVersionCommand { get; private set; }

        public bool NewestOnly { get; private set; }

        public int UiInputInterval { get; private set; }

        public int UiRenderReduction { get; private set; }

        public ReadOnlyCollection<Game.AppType> IgnoredAppTypes { get; }

        public Settings()
        {
            LibraryRoot = Path.Combine(Environment.CurrentDirectory, "Library");
            Log = true;
            ValidateLibraryOnStartup = false;
            Interval = 19 * 60;
            SkipJars = false;
            MigrateOldLibrarianData = "";
            AddedVersionCommand = "";
            NewestOnly = true;
            UiInputInterval = 80;
            UiRenderReduction = 3;
            _ignoredAppTypes = new List<Game.AppType>();
            IgnoredAppTypes = new ReadOnlyCollection<Game.AppType>(_ignoredAppTypes);
        }

        private Settings(Settings settings)
        {
            LibraryRoot = settings.LibraryRoot;
            Log = settings.Log;
            ValidateLibraryOnStartup = settings.ValidateLibraryOnStartup;
            Interval = settings.Interval;
            SkipJars = settings.SkipJars;
            MigrateOldLibrarianData = settings.MigrateOldLibrarianData;
            AddedVersionCommand = settings.AddedVersionCommand;
            NewestOnly = settings.NewestOnly;
            UiInputInterval = settings.UiInputInterval;
            UiRenderReduction = settings.UiRenderReduction;
            _ignoredAppTypes = settings._ignoredAppTypes.ToList();
            IgnoredAppTypes = new ReadOnlyCollection<Game.AppType>(_ignoredAppTypes);
        }

        public Settings([NotNull] string json) : this()
        {
            using JsonDocument jsonDoc = JsonDocument.Parse(json);

            JsonElement tmp;

            if (jsonDoc.RootElement.TryGetProperty(nameof(LibraryRoot), out tmp))
                LibraryRoot = tmp.GetString();

            if (jsonDoc.RootElement.TryGetProperty(nameof(Log), out tmp))
                Log = tmp.GetBoolean();

            if (jsonDoc.RootElement.TryGetProperty(nameof(ValidateLibraryOnStartup), out tmp))
                ValidateLibraryOnStartup = tmp.GetBoolean();

            if (jsonDoc.RootElement.TryGetProperty(nameof(Interval), out tmp) && tmp.TryGetInt32(out int interval))
                Interval = interval;

            if (jsonDoc.RootElement.TryGetProperty(nameof(SkipJars), out tmp))
                SkipJars = tmp.GetBoolean();

            if (jsonDoc.RootElement.TryGetProperty(nameof(AddedVersionCommand), out tmp))
                AddedVersionCommand = tmp.GetString();

            if (jsonDoc.RootElement.TryGetProperty(nameof(NewestOnly), out tmp))
                NewestOnly = tmp.GetBoolean();

            if (jsonDoc.RootElement.TryGetProperty(nameof(UiInputInterval), out tmp) && tmp.TryGetInt32(out int uiInputInterval))
                UiInputInterval = uiInputInterval;

            if (jsonDoc.RootElement.TryGetProperty(nameof(UiRenderReduction), out tmp) && tmp.TryGetInt32(out int uiRenderReduction))
                UiRenderReduction = uiRenderReduction;

            if (jsonDoc.RootElement.TryGetProperty(nameof(IgnoredAppTypes), out tmp))
            {
                string[] appTypes = tmp.ToString().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string appType in appTypes)
                {
                    if (!Enum.TryParse(appType, true, out Game.AppType type) || _ignoredAppTypes.Contains(type))
                        continue;

                    _ignoredAppTypes.Add(type);
                }
            }
        }

        public Settings CreateOverride([NotNull] string[] args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            Settings overridden = new Settings(this);

            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-l":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -l");

                        overridden.LibraryRoot = args[i];
                        break;
                    case "-n":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -n");

                        overridden.Log = bool.Parse(args[i]);
                        break;
                    case "-v":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -v");

                        overridden.ValidateLibraryOnStartup = bool.Parse(args[i]);
                        break;
                    case "-i":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -i");

                        overridden.Interval = int.Parse(args[i]);
                        break;
                    case "-s":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -s");

                        overridden.SkipJars = bool.Parse(args[i]);
                        break;
                    case "-migrate":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -migrate");

                        overridden.MigrateOldLibrarianData = args[i];
                        break;
                    case "-c":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -c");

                        overridden.AddedVersionCommand = args[i];
                        break;
                    case "-e":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -e");

                        overridden.NewestOnly = bool.Parse(args[i]);
                        break;
                    case "-ui":
                        if (i + 2 >= args.Length)
                            throw new Exception("Missing values for -ui");

                        overridden.UiInputInterval = int.Parse(args[++i]);
                        overridden.UiRenderReduction = int.Parse(args[++i]);
                        break;
                    case "-t":
                        ++i;
                        if (i >= args.Length)
                            throw new Exception("Missing value for -t");

                        string[] appTypes = args[i].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string appType in appTypes)
                        {
                            if (!Enum.TryParse(appType, true, out Game.AppType type) || _ignoredAppTypes.Contains(type))
                                continue;

                            overridden._ignoredAppTypes.Add(type);
                        }
                        break;

                    default:
                        throw new Exception($"Unknown argument \"{args[i]}\"");
                }
            }

            return overridden;
        }
    }
}
