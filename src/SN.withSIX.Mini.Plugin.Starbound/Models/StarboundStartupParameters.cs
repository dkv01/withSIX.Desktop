// <copyright company="SIX Networks GmbH" file="StarboundStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Starbound.Models
{
    [DataContract]
    public class StarboundStartupParameters : BasicAltGameStartupParameters
    {
        public StarboundStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public StarboundStartupParameters() {}
    }
}