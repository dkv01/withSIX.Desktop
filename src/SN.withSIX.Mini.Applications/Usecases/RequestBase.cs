// <copyright company="SIX Networks GmbH" file="RequestBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Usecases
{
    // If a command IHaveGameId, then it is assumed it must lock the game. Use this to disable this behavior.
    public interface IExcludeGameWriteLock {}

    // TODO: Get ClientId and RequestId from somewhere
    public interface IRequestBase : IHaveClientId, IHaveRequestId {}

    public interface IAsyncVoidCommandBase : IRequestBase, IAsyncVoidCommand {}

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
        IAsyncVoidCommandBase GetNextAction();
    }

    public interface INotifyAction : IAsyncVoidCommandBase, IHandleAction, IHaveGameId, INeedGameContents {}

    public interface ICancellable
    {
        CancellationToken CancelToken { get; set; }
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