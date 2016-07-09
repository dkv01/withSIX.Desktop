// <copyright company="SIX Networks GmbH" file="Cheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using Caliburn.Micro;
using ShortBus;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Play.Applications
{
    public static class Cheat
    {
        static ICheatImpl _cheat;

        public static void PublishEvent<T>(T evt) => _cheat.EventBus.PublishOnCurrentThread(evt);
        public static Task PublishEventUi<T>(T evt) => _cheat.EventBus.PublishOnUIThreadAsync(evt);

        public static async Task PublishDomainEvent<T>(T evt) {
            _cheat.Mediator.Notify(evt);
            await _cheat.Mediator.NotifyAsync(evt).ConfigureAwait(false);
        }

        public static void SetServices(ICheatImpl cheatImpl) => _cheat = cheatImpl;
    }

    public interface ICheatImpl
    {
        IEventAggregator EventBus { get; }
        IMediator Mediator { get; }
    }

    public class CheatImpl : ICheatImpl, IApplicationService
    {
        public CheatImpl(IEventAggregator eventBus, IMediator mediator) {
            EventBus = eventBus;
            Mediator = mediator;
        }

        public IEventAggregator EventBus { get; }
        public IMediator Mediator { get; }
    }
}