// <copyright company="SIX Networks GmbH" file="Validation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using FluentValidation;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Services
{
    public class GameIdValidator : AbstractValidator<IHaveGameId>
    {
        public GameIdValidator() {
            RuleFor(x => x.GameId)
                .NotEqual(Guid.Empty);
        }
    }
}