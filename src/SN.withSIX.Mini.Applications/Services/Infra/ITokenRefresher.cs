// <copyright company="SIX Networks GmbH" file="ITokenRefresher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Mini.Applications.Models;

namespace SN.withSIX.Mini.Applications.Services.Infra
{
    public interface ITokenRefresher
    {
        //Task Logout();
        Task HandleLogin(AccessInfo info, Settings settings);
    }


    public class AccessInfo
    {
        public string AccessToken { get; set; }
    }


    public class LoginChanged : IDomainEvent
    {
        public LoginChanged(LoginInfo login) {
            LoginInfo = login;
        }

        public LoginInfo LoginInfo { get; }
    }

    public class PremiumTokenUpdatedEvent : IDomainEvent
    {
        public PremiumTokenUpdatedEvent(PremiumAccessToken newToken) {
            NewToken = newToken;
        }

        public PremiumAccessToken NewToken { get; }
    }
}