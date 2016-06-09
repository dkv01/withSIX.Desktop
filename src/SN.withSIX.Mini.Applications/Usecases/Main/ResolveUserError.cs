// <copyright company="SIX Networks GmbH" file="ResolveUserError.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
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

    public class ResolveUserErrorHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<ResolveUserError>
    {
        private readonly IStateHandler _handler;

        public ResolveUserErrorHandler(IDbContextLocator dbContextLocator, IStateHandler handler)
            : base(dbContextLocator) {
            _handler = handler;
        }

        public async Task<UnitType> HandleAsync(ResolveUserError request) {
            await _handler.ResolveError(request.Id, request.Result, request.Data).ConfigureAwait(false);
            return UnitType.Default;
        }
    }
}