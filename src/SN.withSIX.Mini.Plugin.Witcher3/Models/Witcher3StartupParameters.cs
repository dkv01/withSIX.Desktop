// <copyright company="SIX Networks GmbH" file="Witcher3StartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Witcher3.Models
{
    [DataContract]
    public class Witcher3StartupParameters : BasicAltGameStartupParameters
    {
        public Witcher3StartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public Witcher3StartupParameters() {}
    }
}