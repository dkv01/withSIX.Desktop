// <copyright company="SIX Networks GmbH" file="StellarisStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Stellaris.Models
{
    [DataContract]
    public class StellarisStartupParameters : BasicAltGameStartupParameters
    {
        public StellarisStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public StellarisStartupParameters() {}
    }
}