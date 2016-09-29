// <copyright company="SIX Networks GmbH" file="OpenWebLink.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Applications.Usecases
{
    public class OpenWebLink : IAsyncVoidCommand
    {
        public OpenWebLink(ViewType type, string additional = null) {
            Type = type;
            Additional = additional;
        }

        public string Additional { get; }
        public ViewType Type { get; }
    }

    public class OpenArbWebLink : IAsyncVoidCommand
    {
        public OpenArbWebLink(Uri uri) {
            Uri = uri;
        }

        public Uri Uri { get; }
    }

    // TODO: Navigation is normally a Query, not a Command...
    // In that case it needs to return the ViewModel so that the caller can display it either by routing to it,
    // or by leveraging a DialogManager to create a Window/Popup/MessageBox etc?
    //
    // One issue with that is that we are normally supposed to pass back and forth data-containers without logic inside them..
    // Which is not the case with WPF ViewModels, as they include: Commands and methods that need to interact with the Mediator, and perhaps a DialogManager?
    // Idea: What if we do pass only data containers back, but then construct the ViewModels on the other side? Kind of like we do in Angular?
    public class OpenViewHandler : IAsyncVoidCommandHandler<OpenWebLink>, IAsyncVoidCommandHandler<OpenArbWebLink>
    {
        public Task<Unit> Handle(OpenArbWebLink request) => UriOpener.OpenUri(request.Uri).Void();

        // TODO: This makes most sense for Online links...
        // Less sense for LOCAL, as most if not all views require queries to be executed to fill the ViewModels...
        // Unless we want to perform those inside the factories, or in here - not too bad idea actually but seems like the enum is then less useful as we can achieve the same with proper query objects?
        public Task<Unit> Handle(OpenWebLink request) => OpenUri(request).Void();

        static Task OpenUri(OpenWebLink request) {
            switch (request.Type) {
            // Online
            case ViewType.Browse:
                return UriOpener.OpenUri(Urls.Play, request.Additional);
            case ViewType.Friends:
                return UriOpener.OpenUri(Urls.Main, "me/friends");
            case ViewType.PremiumAccount:
                return UriOpener.OpenUri(Urls.Main, "me/premium");

            case ViewType.Settings:
                var port = Cheat.Args.Port == null ? "" : $"&port={Cheat.Args.Port}";
                return UriOpener.OpenUri(Urls.Main, "client-landing?openClientSettings=1&sync=1" + port);
            case ViewType.ClientLanding:
                return UriOpener.OpenUri(Urls.Main, "client-landing?sync=1");

            case ViewType.GoPremium:
                return UriOpener.OpenUri(Urls.Main, "gopremium");

            case ViewType.Help:
                return UriOpener.OpenUri(new Uri("http://withsix.readthedocs.org"));

            case ViewType.Profile:
                return UriOpener.OpenUri(Urls.Main, "me/content");

            case ViewType.Issues:
                return UriOpener.OpenUri(new Uri("https://trello.com/b/EQeUdFGd/withsix-report-issues"));
            // Link to comments and feedback instead??

            case ViewType.Suggestions:
                return
                    UriOpener.OpenUri(new Uri("https://community.withsix.com/category/4/comments-feedback"));
            case ViewType.Community:
                return
                    UriOpener.OpenUri(new Uri("https://community.withsix.com"));
            case ViewType.License:
                return UriOpener.OpenUri(Urls.Main, "legal");

            case ViewType.Update:
                return UriOpener.OpenUri(Urls.Main, "update");

            default: {
                throw new NotSupportedException(request.Type + " Is not supported!");
            }
            }
        }
    }

    public enum ViewType
    {
        // Online
        Browse,
        Friends,
        GoPremium,
        PremiumAccount,
        Profile,
        Issues,
        License,
        Update,
        Suggestions,
        Community,
        Help,
        Settings,
        ClientLanding
    }
}