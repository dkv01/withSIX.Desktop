// <copyright company="SIX Networks GmbH" file="Request.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;

namespace withSIX.Core.Applications.Services
{
    public interface IUsecaseExecutor {}

    public interface IRead {}

    public interface IWrite {}

    public interface IAsyncQuery<out T> : IRead, IRequest<T> {}

    public interface IAsyncCommand<out T> : IWrite, IRequest<T> {}

    public interface IAsyncVoidCommand : IWrite, IRequest {}
}