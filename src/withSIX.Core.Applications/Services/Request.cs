// <copyright company="SIX Networks GmbH" file="Request.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;

namespace withSIX.Core.Applications.Services
{
    public interface IUsecaseExecutor {}

    public interface IQuery<out T> : IRead, IRequest<T> {}

    public interface IRead {}

    public interface ICommand<out T> : IWrite, IRequest<T> {}

    public interface IWrite {}

    public interface IVoidCommand : ICommand<Unit> {}

    public interface IVoidCommandHandler<in TCommand> : IRequestHandler<TCommand, Unit>
        where TCommand : IRequest<Unit> {}

    public interface IAsyncQuery<out T> : IRead, IRequest<T> {}

    public interface IAsyncCommand<out T> : IWrite, IRequest<T> {}

    public interface IAsyncVoidCommand : IAsyncCommand<Unit> {}

    [Obsolete("TODO: Convert to no return")]
    public interface IAsyncVoidCommandHandler<in TCommand> : IAsyncRequestHandler<TCommand, Unit>
        where TCommand : IRequest<Unit> {}
}