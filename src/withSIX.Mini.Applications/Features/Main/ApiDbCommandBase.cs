﻿// <copyright company="SIX Networks GmbH" file="ApiDbCommandBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main
{
    public abstract class ApiDbQueryBase : DbQueryBase
    {
        protected ApiDbQueryBase(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}
    }

    public abstract class ApiDbCommandBase : DbCommandBase
    {
        protected ApiDbCommandBase(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}
    }

    public static class RequestExtensions
    {
        public static async Task<T2> NotifyAction<T, T2>(this T request, Func<Task<T2>> action, string text = null,
            Uri href = null)
            where T : IHaveGameId, ICommandBase {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (action == null) throw new ArgumentNullException(nameof(action));
            var r = await request.PerformAction(action, text, href).ConfigureAwait(false);
            await request.HandleSuccessAction(text, href).ConfigureAwait(false);
            return r;
        }

        private static Task HandleSuccessAction<T>(this T request, string text, Uri href)
            where T : IHaveGameId, ICommandBase {
            var info = request.GetInfo();
            var ov = request as IOverrideNotificationTitle;
            var endTitle = ov?.ActionTitleOverride?.GetPastFromVerb() ?? info.Past;
            var successAction = (request as IHaveNexAction)?.GetNextAction();
            var nextAction = successAction == null ? null : CreateNextActionFromRequest(successAction, text, href);
            return request.FromRequest(endTitle, text, href, ActionType.End, nextAction)
                .Raise();
        }

        public static async Task<T2> PerformAction<T, T2>(this T request, Func<Task<T2>> action, string text,
            Uri href = null)
            where T : IHaveGameId, ICommandBase {
            var info = request.GetInfo();
            var ov = request as IOverrideNotificationTitle;
            var startTitle = ov?.ActionTitleOverride?.GetActingFromVerb() ?? info.Acting;
            var abortAction = new Pause(request.GameId).CreateNextActionFromRequest(text, href, ov?.PauseTitleOverride);
            await
                request.FromRequest(startTitle, text, href,
                        nextAction: request is ICancelable ? abortAction : null)
                    .Raise()
                    .ConfigureAwait(false);
            try {
                return await action().ConfigureAwait(false);
            } catch (InstallerSession.AbortedException) {
                await HandleAsFailed(request, text, href, info).ConfigureAwait(false);
                throw;
            } catch (OperationCanceledException) {
                await HandleAsPaused(request, text, href, info, abortAction).ConfigureAwait(false);
                throw;
            } catch {
                await HandleAsFailed(request, text, href, info).ConfigureAwait(false);
                throw;
            }
        }

        private static async Task HandleAsPaused<T>(T request, string text, Uri href,
            NotifyingActionOverrideAttribute info,
            Tuple<NextActionInfo, ICommandBase> abortAction) where T : IHaveGameId, ICommandBase {
            var nextAction = CreateNextActionFromRequest(request, text, href, "Continue");
            await
                request.FromRequest($"{info.Noun} {abortAction.Item1.Title.GetPastFromVerb()}", text,
                    nextAction.Item1.Href, ActionType.Cancel,
                    nextAction).Raise().ConfigureAwait(false);
        }

        private static async Task HandleAsFailed<T>(T request, string text, Uri href,
            NotifyingActionOverrideAttribute info)
            where T : IHaveGameId, ICommandBase {
            var nextAction = CreateNextActionFromRequest(request, text, href, "Retry");
            await
                request.FromRequest($"{info.Noun} {"Fail".GetPastFromVerb()}", text, nextAction.Item1.Href,
                    ActionType.Fail,
                    nextAction).Raise().ConfigureAwait(false);
        }

        private static string GetActionName<T>(this T action) where T : IRequestBase
            => action.GetActionInfo()?.NameOverride ?? action.GetType().Name;

        private static ApiUserActionAttribute GetActionInfo<T>(this T successAction) where T : IRequestBase
            =>
                (ApiUserActionAttribute)
                successAction.GetType().GetTypeInfo().GetCustomAttribute(typeof(ApiUserActionAttribute));

        private static Tuple<NextActionInfo, ICommandBase> CreateNextActionFromRequest<T>(this T request,
            string text = null, Uri href = null,
            string nameOverride = null) where T : ICommandBase =>
            Tuple.Create(
                new NextActionInfo(nameOverride ?? request.GetActionName(), text) {
                    RequestId = request.RequestId,
                    Href = href
                },
                (ICommandBase) request);

        private static NotifyingActionOverrideAttribute GetInfo<T>(this T request) where T : IRequestBase
            => (NotifyingActionOverrideAttribute)
               request.GetType().GetTypeInfo().GetCustomAttribute(typeof(NotifyingActionOverrideAttribute)) ??
               new NotifyingActionOverrideAttribute(GetActionName(request));

        private static ActionNotification FromRequest<T>(this T request, string title, string text, Uri href = null,
            ActionType type = ActionType.Start, Tuple<NextActionInfo, ICommandBase> nextAction = null)
            where T : IHaveGameId, IHaveClientId, IHaveRequestId
            =>
                new ActionNotification(request.GameId, title, text, request.ClientId, request.RequestId) {
                    Type = type,
                    NextAction = nextAction?.Item2,
                    NextActionInfo = nextAction?.Item1,
                    Href = href,
                    DesktopNotification =
                        type != ActionType.Start &&
                        (type != ActionType.End || !(request is IDisableDesktopNotification))
                };
    }

    // TODO: This should rather be configurable on the action attribute instead?
    internal interface IDisableDesktopNotification {}

    // TODO: Specify in Attribute instead?
    public interface ICancelable {}

    public interface IUseContent : IHandleAction {}

    public interface IOverrideNotificationTitle
    {
        string ActionTitleOverride { get; }
        string PauseTitleOverride { get; }
    }

    public class ActionNotification : IAsyncDomainEvent
    {
        public ActionNotification(Guid gameId, string title, string text, Guid clientId, Guid requestId) {
            GameId = gameId;
            Title = title;
            Text = text;
            ClientId = clientId;
            RequestId = requestId;
        }

        public Guid GameId { get; }
        public Guid ClientId { get; }
        public Guid RequestId { get; }
        public string Title { get; }
        public string Text { get; }
        public ActionType Type { get; set; }
        public NextActionInfo NextActionInfo { get; set; }
        [JsonIgnore]
        public ICommandBase NextAction { get; set; }
        public Uri Href { get; set; }
        public bool DesktopNotification { get; set; } = true;
    }

    public enum ActionType
    {
        Start,
        End,
        Fail,
        Cancel
    }
}