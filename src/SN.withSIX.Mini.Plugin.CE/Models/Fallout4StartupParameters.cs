// <copyright company="SIX Networks GmbH" file="Fallout4StartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.CE.Models
{
    [DataContract]
    public class Fallout4StartupParameters : BasicAltGameStartupParameters
    {
        public Fallout4StartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public Fallout4StartupParameters() {}
    }
}