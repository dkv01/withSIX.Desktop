// <copyright company="SIX Networks GmbH" file="SkyrimStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.CE.Models
{
    [DataContract]
    public class SkyrimStartupParameters : BasicAltGameStartupParameters
    {
        public SkyrimStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public SkyrimStartupParameters() {}
    }
}