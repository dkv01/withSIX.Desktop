// <copyright company="SIX Networks GmbH" file="Login.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class Login : IAsyncVoidCommand
    {
        public Login(AccessInfo info) {
            Info = info;
        }

        public AccessInfo Info { get; }
    }

    public class LoginHandler : DbCommandBase, IAsyncVoidCommandHandler<Login>
    {
        private readonly ITokenRefresher _tokenRefresher;

        public LoginHandler(IDbContextLocator dbContextLocator, ITokenRefresher tokenRefresher) : base(dbContextLocator) {
            _tokenRefresher = tokenRefresher;
        }

        public async Task<UnitType> HandleAsync(Login request) {
            await
                _tokenRefresher.HandleLogin(request.Info, await SettingsContext.GetSettings().ConfigureAwait(false))
                    .Void()
                    .ConfigureAwait(false);
            return UnitType.Default;
        }
    }
}