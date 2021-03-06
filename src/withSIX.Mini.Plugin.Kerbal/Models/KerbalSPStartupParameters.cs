// <copyright company="SIX Networks GmbH" file="KerbalSPStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Kerbal.Models
{
    [DataContract]
    public class KerbalSPStartupParameters : UnityStartupParameters
    {
        public KerbalSPStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public KerbalSPStartupParameters() {}
    }
}