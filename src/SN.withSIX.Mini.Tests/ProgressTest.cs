// <copyright company="SIX Networks GmbH" file="ProgressTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Tests
{
    [TestFixture]
    public class ProgressTest : BaseTest<ProgressComponent>
    {
        class Mod
        {
            public string Name { get; set; }
            public int Size { get; set; }
            public ModType Type { get; set; }
        }

        private static Mod[] GetTestMods() => new[] {
            new Mod {Name = "Mod 1", Size = 500},
            new Mod {Name = "Mod 2", Size = 1000},
            new Mod {Name = "Mod 3", Size = 2000, Type = ModType.Group}
        };

        [SetUp]
        public void Setup() {
            MappingExtensions.Mapper = new MapperConfiguration(cfg => {
                cfg.SetupConverters();
                AutoMapperAppConfig.Setup(cfg);
            }).CreateMapper();
        }

        [Test]
        public void TestProgress2() {
            SUT = new ProgressComponent("Stage");
            var networkMods = new ProgressComponent("Network mods");
            var networkModsDownloads = new ProgressLeaf("Downloading").Finished();
            var networkModsExtraction = new ProgressComponent("Extracting");
            networkModsExtraction.AddComponents(new ProgressLeaf("Mod 1").Finished(),
                new ProgressLeaf("Mod 2"));
            networkMods.AddComponents(networkModsDownloads, networkModsExtraction);
            var groupMods = new ProgressComponent("Group mods");
            groupMods.AddComponents(new ProgressLeaf("Mod 3"));
            SUT.AddComponents(networkMods, groupMods);

            networkModsDownloads.Progress.Should().Be(100);
            networkModsExtraction.Progress.Should().Be(50);
            networkMods.Progress.Should().Be(75);
            groupMods.Progress.Should().Be(0);
            SUT.Progress.Should().Be(37.5);
        }

        [Test]
        public void TestProgressDownloading() {
            SUT = new ProgressComponent("Stage");
            var networkMods = new ProgressComponent("Network mods");
            var networkModsDownloads = new ProgressLeaf("Downloading");
            networkModsDownloads.Update(500, 50);
            networkMods.AddComponents(networkModsDownloads);
            var groupMods = new ProgressComponent("Group mods");
            groupMods.AddComponents(new ProgressLeaf("Mod 3"));
            SUT.AddComponents(networkMods, groupMods);

            SUT.StatusText.Should().Be("Stage 1/2 25%\nNetwork mods 1/1 50%\nDownloading 50% @ 500 B/s");
        }

        [Test]
        public void TestProgressWithCustomWeight() {
            SUT = new ProgressComponent("Stage");

            var mods = GetTestMods();

            var nMods = mods.Where(x => x.Type == ModType.Network).ToArray();

            var networkMods = new ProgressComponent("Network mods", nMods.Length);
            var networkModsDownloads = new ProgressLeaf("Downloading").Finished();
            var networkModsExtraction = new ProgressComponent("Extracting");
            networkModsExtraction.AddComponents(new ProgressLeaf(nMods[0].Name).Finished(),
                new ProgressLeaf(nMods[1].Name));
            networkMods.AddComponents(networkModsDownloads, networkModsExtraction);
            SUT.AddComponents(networkMods);

            var gMods = mods.Where(x => x.Type == ModType.Group).ToArray();
            var groupMods = new ProgressComponent("Group mods", gMods.Length);
            var groupModsProcessing = new ProgressComponent("Processing");
            groupModsProcessing.AddComponents(gMods.Select(x => new ProgressLeaf(x.Name)).ToArray());
            groupMods.AddComponents(groupModsProcessing);
            SUT.AddComponents(groupMods);

            networkModsDownloads.Progress.Should().Be(100);
            networkModsExtraction.Progress.Should().Be(50);
            networkMods.Progress.Should().Be(75);
            groupMods.Progress.Should().Be(0);
            groupModsProcessing.Progress.Should().Be(0);
            SUT.Progress.Should().Be(50);
            SUT.StatusText.Should().Be("Stage 1/2 50%\nNetwork mods 2/2 75%\nExtracting 2/2 50%\nDone");
        }

        [Test]
        public void FlatProgressTest() {
            SUT = new ProgressComponent("Stage");
            var networkMods = new ProgressComponent("Network mods");
            var networkModsDownloads = new ProgressLeaf("Downloading").Finished();
            var networkModsExtraction = new ProgressComponent("Extracting");
            networkModsExtraction.AddComponents(new ProgressLeaf("Mod 1").Finished(),
                new ProgressLeaf("Mod 2"));
            networkMods.AddComponents(networkModsDownloads, networkModsExtraction);
            var groupMods = new ProgressComponent("Group mods");
            groupMods.AddComponents(new ProgressLeaf("Mod 3"));
            SUT.AddComponents(networkMods, groupMods);

            var sut = SUT.Flatten();

            sut[0].Title.Should().Be("Stage");
            sut[0].Progress.Should().Be(37.5);
            sut[0].ComponentsCount.Should().Be(2);
            sut[0].CurrentStage.Should().Be(1);
            sut[0].Speed.Should().Be(null);

            sut[1].Title.Should().Be("Network mods");
            sut[1].Progress.Should().Be(75);
            sut[1].ComponentsCount.Should().Be(2);
            sut[1].CurrentStage.Should().Be(2);
            sut[1].Speed.Should().Be(null);

            sut[2].Title.Should().Be("Extracting");
            sut[2].Progress.Should().Be(50);
            sut[2].ComponentsCount.Should().Be(2);
            sut[2].CurrentStage.Should().Be(2);
            sut[2].Speed.Should().Be(null);
        }


        [Test]
        public void ProgressDownloadingFlatTest() {
            SUT = new ProgressComponent("Stage");
            var networkMods = new ProgressComponent("Network mods");
            var networkModsDownloads = new ProgressLeaf("Downloading");
            networkModsDownloads.Update(500, 50);
            networkMods.AddComponents(networkModsDownloads);
            var groupMods = new ProgressComponent("Group mods");
            groupMods.AddComponents(new ProgressLeaf("Mod 3"));
            SUT.AddComponents(networkMods, groupMods);

            var sut = SUT.Flatten();

            sut[0].Title.Should().Be("Stage");
            sut[0].Progress.Should().Be(25);
            sut[0].ComponentsCount.Should().Be(2);
            sut[0].CurrentStage.Should().Be(1);
            sut[0].Speed.Should().Be(500);

            sut[1].Title.Should().Be("Network mods");
            sut[1].Progress.Should().Be(50);
            sut[1].ComponentsCount.Should().Be(1);
            sut[1].CurrentStage.Should().Be(1);
            sut[1].Speed.Should().Be(500);

            sut[2].Title.Should().Be("Downloading");
            sut[2].Progress.Should().Be(50);
            sut[2].ComponentsCount.Should().Be(0);
            sut[2].CurrentStage.Should().Be(0);
            sut[2].Speed.Should().Be(500);
        }
    }

    internal enum ModType
    {
        Network,
        Group,
        Repo
    }
}