// <copyright company="SIX Networks GmbH" file="CarrierCommandGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Play.Core.Games.Services.GameLauncher;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Games.Entities.Other
{
    public class CarrierCommandGame : Game, ILaunchWith<IBasicGameLauncher>
    {
        static readonly GameMetaData metaData = new GameMetaData {
            Name = "Carrier Command: Gaea Mission",
            ShortName = "Carrier Command",
            Author = "Bohemia Interactive",
            Description =
                @"Carrier Command: Gaea Mission is a breathtaking combination of action game with strategic elements set in a vast detailed archipelago. It reinvents the classic gameplay of the original Carrier Command to deliver a truly next-gen experience.",
            Slug = "carrier-command",
            StoreUrl = "https://store.bistudio.com/strategy-games?banner_tag=SIXNetworks".ToUri(),
            SupportUrl = @"http://www.carriercommand.com/support".ToUri(),
            ReleasedOn = new DateTime(2009, 1, 1)
        };

        public CarrierCommandGame(Guid id, GameSettingsController settingsController)
            : base(
                id,
                new CarrierCommandSettings(id, new CarrierCommandStartupParmeters(DefaultStartupParameters),
                    settingsController)) {}

        public override GameMetaData MetaData => metaData;

        protected override IAbsoluteFilePath GetExecutable() => GetFileInGameDirectory("carrier.exe");

        public override Task<IReadOnlyCollection<string>> ShortcutLaunchParameters(IGameLauncherFactory factory,
            string identifier) {
            throw new NotImplementedException();
        }

        public override Task<int> Launch(IGameLauncherFactory factory) => LaunchBasic(factory.Create(this));

        protected override string GetStartupLine() => Settings.StartupParameters.Get().CombineParameters();
    }
}