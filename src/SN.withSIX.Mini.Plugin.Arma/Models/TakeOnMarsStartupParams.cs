// <copyright company="SIX Networks GmbH" file="TakeOnMarsStartupParams.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Arma.Models
{
    // TODO
    [DataContract]
    public class TakeOnMarsStartupParams : BasicGameStartupParameters
    {
        public TakeOnMarsStartupParams(string[] defaultParameters) : base(defaultParameters) {}
        public TakeOnMarsStartupParams() {}
    }
}