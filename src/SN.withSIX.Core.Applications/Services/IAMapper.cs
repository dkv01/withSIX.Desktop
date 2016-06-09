// <copyright company="SIX Networks GmbH" file="IAMapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Applications.Services
{
    public interface IAMapper
    {
        TOut Map<TOut>(object input);
        TOut Map<TIn, TOut>(TIn input, TOut output);
    }
}