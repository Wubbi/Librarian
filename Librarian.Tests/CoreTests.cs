using System;
using Librarian.Core;
using NUnit.Framework;

namespace Librarian.Tests
{
    [TestFixture]
    public class CoreTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            Mocks.MockWebResourceHttpClient();
        }

        [Test]
        public void Game_Parse()
        {
            string source = @"
                {
                  ""downloads"": {
                    ""client"": {
                      ""sha1"": ""8c325a0c5bd674dd747d6ebaa4c791fd363ad8a9"",
                      ""size"": 25191691,
                      ""url"": ""https://launcher.mojang.com/v1/objects/8c325a0c5bd674dd747d6ebaa4c791fd363ad8a9/client.jar""
                    },
                    ""server"": {
                      ""sha1"": ""3dc3d84a581f14691199cf6831b71ed1296a9fdf"",
                      ""size"": 35958734,
                      ""url"": ""https://launcher.mojang.com/v1/objects/3dc3d84a581f14691199cf6831b71ed1296a9fdf/server.jar""
                    }
                  },
                  ""id"": ""1.14.4"",
                  ""mainClass"": ""net.minecraft.client.main.Main"",
                  ""minimumLauncherVersion"": 21,
                  ""releaseTime"": ""2019-07-19T09:25:47+00:00"",
                  ""time"": ""2019-07-19T09:25:47+00:00"",
                  ""type"": ""release""
                }";

            DateTime timestamp = DateTime.Parse("2019-07-19T09:25:47+00:00");

            Game game = new Game(source);
            Assert.AreEqual("1.14.4", game.Id);
            Assert.AreEqual(Game.BuildType.Release, game.Type);
            Assert.AreEqual(timestamp, game.ReleaseTime);
            Assert.AreEqual(timestamp, game.Time);

            Assert.AreEqual(2, game.Downloads.Count);
            Assert.Contains(Game.AppType.Client, game.Downloads.Keys);
            Assert.Contains(Game.AppType.Server, game.Downloads.Keys);

            Assert.AreEqual("8c325a0c5bd674dd747d6ebaa4c791fd363ad8a9", game.Downloads[Game.AppType.Client].Sha1);
            Assert.AreEqual(25191691, game.Downloads[Game.AppType.Client].Size);
            Assert.AreEqual("https://launcher.mojang.com/v1/objects/8c325a0c5bd674dd747d6ebaa4c791fd363ad8a9/client.jar", game.Downloads[Game.AppType.Client].Url);

            Assert.AreEqual("3dc3d84a581f14691199cf6831b71ed1296a9fdf", game.Downloads[Game.AppType.Server].Sha1);
            Assert.AreEqual(35958734, game.Downloads[Game.AppType.Server].Size);
            Assert.AreEqual("https://launcher.mojang.com/v1/objects/3dc3d84a581f14691199cf6831b71ed1296a9fdf/server.jar", game.Downloads[Game.AppType.Server].Url);
        }

        [Test]
        public void WebResource_Download()
        {
            byte[] data = { 0, 1, 2 };
            Mocks.SetWebResourceContent(data);

            int updateCount = 0;
            WebResource webResource = WebResource.LoadAsync("https://launcher.mojang.com/v1/objects/8c325a0c5bd674dd747d6ebaa4c791fd363ad8a9/client.jar"
            , p => ++updateCount
            ).Result;

            Assert.AreEqual(2, updateCount);

            Assert.IsTrue(webResource.Completed);
            Assert.AreEqual("https://launcher.mojang.com/v1/objects/8c325a0c5bd674dd747d6ebaa4c791fd363ad8a9/client.jar", webResource.Url);
            Assert.AreEqual("0c7a623fd2bbc05b06423be359e4021d36e721ad", webResource.Sha1);

            CollectionAssert.AreEqual(data, webResource.Data.ToArray());
        }

        [Test]
        public void Inventory_Parse()
        {
            string source = @"
                {
                  ""latest"": {
                    ""release"": ""1.14.4"",
                    ""snapshot"": ""19w44a""
                  },
                  ""versions"": [
                    {
                      ""id"": ""19w44a"",
                      ""type"": ""snapshot"",
                      ""url"": ""https://launchermeta.mojang.com/v1/packages/0e02a0f95ccc2649565cd30b096fa2257d3f2182/19w44a.json"",
                      ""time"": ""2019-10-30T15:41:50+00:00"",
                      ""releaseTime"": ""2019-10-30T15:31:44+00:00""
                    },
                    {
                      ""id"": ""1.14.2"",
                      ""type"": ""release"",
                      ""url"": ""https://launchermeta.mojang.com/v1/packages/2a74bf61101997340b101677434b36c29af68bd7/1.14.2.json"",
                      ""time"": ""2019-08-22T12:46:36+00:00"",
                      ""releaseTime"": ""2019-05-27T11:48:25+00:00""
                    },
                    {
                      ""id"": ""rd-132211"",
                      ""type"": ""old_alpha"",
                      ""url"": ""https://launchermeta.mojang.com/v1/packages/0f2a46082313d0ec67972f9f63c3fa6591f9bb85/rd-132211.json"",
                      ""time"": ""2019-06-28T07:05:42+00:00"",
                      ""releaseTime"": ""2009-05-13T20:11:00+00:00""
                    }
                  ]
                }";

            WebInventory webInventory = new WebInventory(source);

            Assert.AreEqual(2, webInventory.Latest.Count);
            Assert.AreEqual("1.14.4", webInventory.Latest[Game.BuildType.Release]);
            Assert.AreEqual("19w44a", webInventory.Latest[Game.BuildType.Snapshot]);

            Assert.AreEqual(3, webInventory.Versions.Count);
            Assert.AreEqual("19w44a", webInventory.Versions[0].Id);
            Assert.AreEqual("1.14.2", webInventory.Versions[1].Id);
            Assert.AreEqual("rd-132211", webInventory.Versions[2].Id);
        }
    }
}
