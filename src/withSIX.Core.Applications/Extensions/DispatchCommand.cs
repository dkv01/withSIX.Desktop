// <copyright company="SIX Networks GmbH" file="DispatchCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;

namespace withSIX.Core.Applications.Extensions
{
    public interface IDispatchCommand
    {
        Task Dispatch(IMediator mediator);
    }

    public class DispatchCommand<T> : IDispatchCommand
        where T : IRequest<Unit>
    {
        readonly T _command;

        public DispatchCommand(T command) {
            _command = command;
        }

        public Task Dispatch(IMediator mediator) => mediator.Send(_command);
    }
}