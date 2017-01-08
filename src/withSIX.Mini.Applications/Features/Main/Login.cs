// <copyright company="SIX Networks GmbH" file="Login.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class Login : IVoidCommand
    {
        public Login(AccessInfo info) {
            Info = info;
        }

        public AccessInfo Info { get; }
    }

    public class LoginHandler : DbCommandBase, IAsyncRequestHandler<Login>
    {
        private readonly ITokenRefresher _tokenRefresher;

        public LoginHandler(IDbContextLocator dbContextLocator, ITokenRefresher tokenRefresher) : base(dbContextLocator) {
            _tokenRefresher = tokenRefresher;
        }

        public async Task Handle(Login request) {
            await
                _tokenRefresher.HandleLogin(request.Info, await SettingsContext.GetSettings().ConfigureAwait(false))
                    .Void()
                    .ConfigureAwait(false);
        }
    }
}