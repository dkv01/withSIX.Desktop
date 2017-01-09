// <copyright company="SIX Networks GmbH" file="RequestBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using Newtonsoft.Json;
using withSIX.Api.Models.Attributes;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features
{
    // If a command IHaveGameId, then it is assumed it must lock the game. Use this to disable this behavior.
    public interface IExcludeGameWriteLock {}

    // TODO: Get ClientId and RequestId from somewhere
    public interface IRequestBase : IHaveClientId, IHaveRequestId {}

    public interface ICommandBase : IRequestBase, ICommand {}

    public interface INeedGameContents : IHaveGameId {}

    public abstract class RequestBase : IRequestBase
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public Guid ClientId { get; set; }
    }

    public interface IHaveClientId
    {
        Guid ClientId { get; }
    }

    public interface IHaveRequestId
    {
        Guid RequestId { get; }
    }

    public interface IHandleAction
    {
        IContentAction<IContent> GetAction(Game game);
    }

    public interface IHaveNexAction
    {
        ICommandBase GetNextAction();
    }

    public interface INotifyAction : ICommandBase, IHandleAction, IHaveGameId, INeedGameContents {}

    public interface ICancellable
    {
        [JsonIgnore]
        CancellationToken CancelToken { get; set; }
    }

    public interface ICancellable2
    {
        [JsonIgnore]
        CancellationToken CancelToken { get; set; }
        [ValidUuid]
        Guid RequestId { get; set; }
    }

    public class NotifyingActionOverrideAttribute : Attribute
    {
        public NotifyingActionOverrideAttribute(string verb, string acting = null, string past = null) {
            Contract.Requires<ArgumentNullException>(verb != null);
            Verb = verb;
            Noun = verb.GetNounFromVerb();
            Acting = acting ?? verb.GetActingFromVerb();
            Past = past ?? verb.GetPastFromVerb();
        }

        public string Verb { get; }
        public string Noun { get; }
        public string Acting { get; }
        public string Past { get; }
    }
}