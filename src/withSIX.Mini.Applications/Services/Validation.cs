using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
