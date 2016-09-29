// <copyright company="SIX Networks GmbH" file="NMSStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.NMS.Models
{
    [DataContract]
    public class NMSStartupParameters : BasicAltGameStartupParameters
    {
        public NMSStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public NMSStartupParameters() {}
    }
}