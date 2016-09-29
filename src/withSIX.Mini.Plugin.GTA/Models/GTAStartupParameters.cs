// <copyright company="SIX Networks GmbH" file="GTAStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.GTA.Models
{
    [DataContract]
    public abstract class GTAStartupParameters : BasicAltGameStartupParameters
    {
        protected GTAStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        protected GTAStartupParameters() {}
    }
}