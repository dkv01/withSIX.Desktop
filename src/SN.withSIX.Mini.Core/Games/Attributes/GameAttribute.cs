// <copyright company="SIX Networks GmbH" file="GameAttribute.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Core.Games.Attributes
{
    public class GameAttribute : Attribute, IHaveId<Guid>
    {
        public GameAttribute(string id) : this(new Guid(id)) {}

        public GameAttribute(Guid id) {
            Id = id;
            Image = Tools.Transfer.JoinUri(CommonUrls.ImageCdn, "games", Id + "-" + "40x40" + ".png");
            BackgroundImage = Tools.Transfer.JoinUri(CommonUrls.ImageCdn, "games", Id + "-" + "full" + ".png");
            Dlcs = new string[0];
        }

        public bool IsPublic { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Slug { get; set; }
        public string FirstTimeRunInfo { get; set; }
        public string[] Executables { get; set; } = {};
        public string[] MultiplayerExecutables { get; set; } = {};
        public string[] ServerExecutables { get; set; } = {};
        public LaunchType[] LaunchTypes { get; set; } = {LaunchType.Default};
        public Uri Image { get; set; }
        public Uri BackgroundImage { get; set; }
        public string[] Dlcs { get; set; }
        public TimeSpan? AfterLaunchDelay { get; set; } = TimeSpan.FromSeconds(10);
        public Guid Id { get; }

        public IEnumerable<IRelativeFilePath> GetExecutables() => Executables.ToRelativeFilePaths();

        public IEnumerable<IRelativeFilePath> GetMultiplayerExecutables()
            => MultiplayerExecutables.ToRelativeFilePaths();

        public IEnumerable<IRelativeFilePath> GetServerExecutables() => ServerExecutables.ToRelativeFilePaths();

        public IEnumerable<IRelativeFilePath> GetAllExecutables()
            => GetExecutables().Concat(GetMultiplayerExecutables()).Concat(GetServerExecutables());
    }
}