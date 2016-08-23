// <copyright company="SIX Networks GmbH" file="StarboundStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.NMS.Models
{
    [DataContract]
    public class NMSStartupParameters : BasicAltGameStartupParameters
    {
        public NMSStartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public NMSStartupParameters() {}
    }
}