// <copyright company="SIX Networks GmbH" file="Cheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using Caliburn.Micro;
using MediatR;

namespace withSIX.Play.Applications
{
    public static class Cheat
    {
        static ICheatImpl _cheat;

        public static void PublishEvent(object evt) => _cheat.EventBus.PublishOnCurrentThread(evt);
        public static Task PublishEventUi(object evt) => _cheat.EventBus.PublishOnUIThreadAsync(evt);

        public static async Task PublishDomainEvent(INotification evt) => _cheat.Mediator.Publish(evt);
        public static Task PublishDomainEvent(IAsyncNotification evt) => _cheat.Mediator.PublishAsync(evt);

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