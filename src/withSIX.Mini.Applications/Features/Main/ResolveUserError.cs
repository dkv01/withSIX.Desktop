// <copyright company="SIX Networks GmbH" file="ResolveUserError.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction]
    public class ResolveUserError : IAsyncVoidCommand
    {
        public ResolveUserError(Guid id, string result) {
            Id = id;
            Result = result;
        }

        public Guid Id { get; }
        public string Result { get; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class ResolveUserErrorHandler : ApiDbCommandBase, IAsyncRequestHandler<ResolveUserError>
    {
        private readonly IStateHandler _handler;

        public ResolveUserErrorHandler(IDbContextLocator dbContextLocator, IStateHandler handler)
            : base(dbContextLocator) {
            _handler = handler;
        }

        public async Task Handle(ResolveUserError request) {
            await _handler.ResolveError(request.Id, request.Result, request.Data).ConfigureAwait(false);
            
        }
    }
}