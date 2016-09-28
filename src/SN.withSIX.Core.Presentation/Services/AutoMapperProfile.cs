// <copyright company="SIX Networks GmbH" file="AutoMapperProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using AutoMapper;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation.Services;

namespace SN.withSIX.Core.Applications
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<UserErrorModel, UserError>()
                .ConstructUsing((src, dest) => {
                    return new UserError(src.ErrorMessage, src.ErrorCauseOrResolution,
                        src.RecoveryOptions.Select(x => new RecoveryCommandImmediate(x.CommandName,
                            a => x.Handler(a).ConvertOption())), src.ContextInfo, src.InnerException);
                })
                .ForMember(x => x.RecoveryOptions, opt => opt.Ignore());
        }
    }
}