// <copyright company="SIX Networks GmbH" file="BuiltInServerContainer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class BuiltInServerContainer : BuiltInContainer<Server>
    {
        public BuiltInServerContainer(string name) : base(name) {}
    }
}