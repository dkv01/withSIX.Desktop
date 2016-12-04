// <copyright company="SIX Networks GmbH" file="SetLogin.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class SetLogin : IAsyncVoidCommand
    {
        public SetLogin(string apiKey) {
            ApiKey = apiKey;
        }

        public string ApiKey { get; }
    }

    public class SetLoginHandler : DbCommandBase, IAsyncVoidCommandHandler<SetLogin>
    {
        public SetLoginHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(SetLogin request) {
            (await SettingsContext.GetSettings().ConfigureAwait(false)).Secure.Login.Authentication.AccessToken =
                request.ApiKey;
            // TODO: Now handle the AccountInfo + PremiumToken
            // TODO: Refresh token when webbrowser refreshes...

            return Unit.Value;
        }
    }
}