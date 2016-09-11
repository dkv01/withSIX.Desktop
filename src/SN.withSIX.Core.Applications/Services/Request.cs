﻿// <copyright company="SIX Networks GmbH" file="Request.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using MediatR;

namespace SN.withSIX.Core.Applications.Services
{
    public interface IQuery<out T> : IRead, IRequest<T> {}

    public interface IRead {}

    public interface ICommand<out T> : IWrite, IRequest<T> {}

    public interface IWrite {}

    public interface IVoidCommand : ICommand<Unit> {}

    public interface IVoidCommandHandler<in TCommand> : IRequestHandler<TCommand, Unit>
        where TCommand : IRequest<Unit> {}

    public interface IAsyncQuery<out T> : IRead, IAsyncRequest<T> {}

    public interface IAsyncCommand<out T> : IWrite, IAsyncRequest<T> {}

    public interface IAsyncVoidCommand : IAsyncCommand<Unit> {}

    public interface IAsyncVoidCommandHandler<in TCommand> : IAsyncRequestHandler<TCommand, Unit>
        where TCommand : IAsyncRequest<Unit> {}
}