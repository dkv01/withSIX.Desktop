// <copyright company="SIX Networks GmbH" file="DataCheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public static class DataCheat
    {
        public static ICallContextService Instance { get; set; }
    }
}