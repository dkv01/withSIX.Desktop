// <copyright company="SIX Networks GmbH" file="ServerQueryFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications.Factories;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Factories
{
    class ServerQueryFactory : AbstractFactory, IServerQueryFactory, IApplicationService
    {
        public ServerQueryFactory(IDepResolver depResolver) : base(depResolver) {}
        public T Create<T>(IServerQueryWith<T> game) where T : class, IServerQuery => GetInstance<T>();
    }
}