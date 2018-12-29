using System;
using System.IO;
using System.Threading;
using Librarian;
using NUnit.Framework;

namespace LibrarianTests
{
    public class Tests
    {
        [Test]
        public void TestFileDownload()
        {
            const string versionManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
            const string versionManifestDefaultName = "version_manifest.json";
            const string versionManifestGivenName = "version_manifest_rename.json";

            File.Delete(versionManifestDefaultName);
            File.Delete(versionManifestGivenName);

            Assert.Throws<ArgumentException>(() =>
            {
                WebAccess.DownloadAndStoreFile("something.example.test/page?a=24");
            });

            Assert.Throws<InvalidDataException>(() =>
            {
                WebAccess.DownloadAndStoreFile(versionManifestUrl, null, 1);
            });

            WebAccess.DownloadAndStoreFile(versionManifestUrl);
            FileAssert.Exists(versionManifestDefaultName);

            string downloadedFileName = WebAccess.DownloadAndStoreFile(versionManifestUrl, versionManifestGivenName);
            Assert.AreEqual(versionManifestGivenName, downloadedFileName);
            FileAssert.Exists(versionManifestGivenName);

            string downloadedFileAsString = WebAccess.DownloadFileAsString(versionManifestUrl);
            Assert.True(downloadedFileAsString.Length > 0);
        }

        [Test]
        public void TestGameVersionParsing()
        {
            const string snippet = @"{
                                      ""id"": ""18w50a"",
                                      ""type"": ""snapshot"",
                                      ""url"": ""https://launchermeta.mojang.com/v1/packages/bb3dedc8edfa074b4d5a6a483ff073801dde6479/18w50a.json"",
                                      ""time"": ""2018-12-12T16:58:33+01:00"",
                                      ""releaseTime"": ""2018-12-12T15:58:13+01:00""
                                    }";

            GameVersion gameVersion = new GameVersion(snippet);

            Assert.AreEqual("18w50a", gameVersion.Id);
            Assert.AreEqual(GameVersion.BuildType.Snapshot, gameVersion.Type);
            Assert.AreEqual("https://launchermeta.mojang.com/v1/packages/bb3dedc8edfa074b4d5a6a483ff073801dde6479/18w50a.json", gameVersion.VersionMetadataUrl);
            Assert.AreEqual(new DateTime(2018, 12, 12, 16, 58, 33, DateTimeKind.Utc), gameVersion.TimeOfPublication);
            Assert.AreEqual(new DateTime(2018, 12, 12, 15, 58, 13, DateTimeKind.Utc), gameVersion.TimeOfUpload);

            Assert.AreEqual("https://launcher.mojang.com/v1/objects/865a610fe77eb9d2fea48de1a02229526a391249/client.jar", gameVersion.ClientDownloadUrl);
            Assert.AreEqual(17751566L, gameVersion.ClientDownloadSize);
            Assert.AreEqual("https://launcher.mojang.com/v1/objects/de0577900a9071758d7f1172dd283bdbe88b7431/server.jar", gameVersion.ServerDownloadUrl);
            Assert.AreEqual(35155114L, gameVersion.ServerDownloadSize);

        }

        [Test]
        public void TestLauncherInventoryLoad()
        {
            LauncherInventory launcherInventory = new LauncherInventory();

            Assert.NotNull(launcherInventory.LatestReleaseId);
            Assert.NotNull(launcherInventory.LatestSnapshotId);
            Assert.True(launcherInventory.AvailableVersions.Count > 0);
        }

        [Test]
        public void TestWatcherBaseFunctions()
        {
            ManifestWatcher watcher = new ManifestWatcher();
            Assert.NotNull(watcher.CurrentInventory);
            Thread.Sleep(1000);
            watcher.Start(TimeSpan.FromHours(1));
            Thread.Sleep(1000);
            watcher.Start(TimeSpan.FromHours(2));
            Thread.Sleep(1000);
            watcher.Stop();
            watcher.Dispose();
            Assert.Throws<ObjectDisposedException>(() => watcher.Start(TimeSpan.FromDays(1)));
        }

        [Test]
        public void TestSettingsParsing()
        {
            const string json = @"{
	                                refreshRate:30,
	                                libraryPath:""G:/Library"",
	                                addMissingVersions:false,
	                                tasks:
	                                [
		                                {
			                                beforeDownload:true,
			                                dependedOnIds:[],
			                                type:""Snapshot"",
			                                onLatest:false,
			                                onAdded:true,
			                                onChanged:true,
			                                onRemoved:false,
			                                commands:
			                                [
				                                ""echo Snapshot version $id is out!"",
				                                ""echo It will be stored in $path""
			                                ],
			                                params:
			                                {
				                                id:""$id"",
				                                path:""$path""
			                                }
		                                },
		                                {
			                                beforeDownload:false,
			                                dependedOnIds:[],
			                                type:""Snapshot"",
			                                onLatest:false,
			                                onAdded:true,
			                                onChanged:false,
			                                onRemoved:false,
			                                commands:
			                                [
				                                ""echo It was stored in $path""
			                                ],
			                                params:
			                                {
				                                path:""$path""
			                                }
		                                }
	                                ]
                                }";

            Settings settings = new Settings(json);

            Assert.AreEqual(30, settings.ManifestRefreshRate);
            Assert.AreEqual("G:/Library", settings.LibraryPath);
            Assert.AreEqual(false, settings.ProcessMissedUpdates);
            Assert.AreEqual(2, settings.ConditionalActions.Count);
        }
    }
}