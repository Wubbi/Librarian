using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Librarian.Core
{
    public class LocalInventory
    {
        public static readonly LocalInventory Empty;

        static LocalInventory()
        {
            Empty = new LocalInventory();
        }

        public string Root { get; }

        private readonly List<Game> _versions;

        public ReadOnlyCollection<Game> Versions { get; }

        private LocalInventory()
        {
            Root = "";
            _versions = new List<Game>();
            Versions = new ReadOnlyCollection<Game>(_versions);
        }

        public LocalInventory([NotNull] string root)
        {
            if (root is null)
                throw new ArgumentNullException(nameof(root));

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            Root = root;

            _versions = new List<Game>();

            Versions = new ReadOnlyCollection<Game>(_versions);
        }

        public void Scan(Action<string> currentFolderCallback, CancellationToken token)
        {
            Logger.Instance.Log($"Scanning library in \"{Root}\"");

            _versions.Clear();

            foreach (Game.BuildType buildType in Enum.GetValues(typeof(Game.BuildType)).Cast<Game.BuildType>())
            {
                string buildTypeFolder = Path.Combine(Root, Game.BuildTypeToString(buildType));

                if (!Directory.Exists(buildTypeFolder))
                    continue;

                foreach (string versionFolder in Directory.EnumerateDirectories(buildTypeFolder))
                {
                    if (token.IsCancellationRequested)
                        return;

                    currentFolderCallback?.Invoke(versionFolder);
                    string meta = Path.Combine(versionFolder, "meta.json");
                    if (!File.Exists(meta))
                    {
                        Logger.Instance.Log($"Empty folder \"{versionFolder}\"", Logger.Level.Warning);
                        continue;
                    }

                    try
                    {
                        string json = File.ReadAllText(meta);
                        _versions.Add(new Game(json));
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.Log($"Problem while loading data from \"{meta}\": {e}", Logger.Level.Error);
                    }
                }
            }

            Logger.Instance.Log($"Finished scan, found {_versions.Count} versions");
        }

        public async Task CheckIncompleteOrInvalidAsync([NotNull] ScanProgressUpdate updateCallback, CancellationToken token)
        {
            Logger.Instance.Log("Checking library for missing or corrupted jars");

            ScanProgress.Factory factory = new ScanProgress.Factory(Versions.Count);
            updateCallback?.Invoke(factory.Update(0));

            HashSet<Game> invalidGames = new HashSet<Game>();

            int count = 0;
            foreach (Game game in Versions)
            {
                foreach ((Game.AppType appType, Game.DownloadMeta downloadMeta) in game.Downloads)
                {
                    if (token.IsCancellationRequested)
                        return;

                    string file = Path.Combine(GetGameFolder(game), $"{appType}.jar");

                    if (File.Exists(file))
                    {
                        using LocalResource localResource = await LocalResource.LoadAsync(file);

                        if (localResource.Size == downloadMeta.Size && localResource.Sha1.Equals(downloadMeta.Sha1))
                            continue;
                    }


                    invalidGames.Add(game);
                    break;
                }

                updateCallback?.Invoke(factory.Update(++count));
            }

            _versions.RemoveAll(game => invalidGames.Contains(game));

            Logger.Instance.Log($"Found {invalidGames.Count} invalid versions");
        }

        public void AddGame([NotNull] Game meta)
            => AddGame(meta, new Dictionary<Game.AppType, IResource>());

        public void AddGame([NotNull] Game meta, [NotNull] Dictionary<Game.AppType, IResource> data)
        {
            Logger.Instance.Log($"Adding version {meta}");

            if (meta is null)
                throw new ArgumentNullException(nameof(meta));

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            string gameFolder = GetGameFolder(meta);

            if (!Directory.Exists(gameFolder))
                Directory.CreateDirectory(gameFolder);

            File.WriteAllText(Path.Combine(gameFolder, "meta.json"), meta.Json, Encoding.UTF8);

            foreach ((Game.AppType appType, IResource resource) in data)
            {
                using FileStream fileStream = new FileStream(Path.Combine(gameFolder, $"{appType}.jar"), FileMode.Create, FileAccess.Write, FileShare.None);
                resource.Data.CopyTo(fileStream);
            }

            if (!_versions.Contains(meta))
                _versions.Add(meta);
        }

        public string GetGameFolder([NotNull] Game game)
        {
            return Path.Combine(Root, Game.BuildTypeToString(game.Type), game.Id);
        }
    }
}
