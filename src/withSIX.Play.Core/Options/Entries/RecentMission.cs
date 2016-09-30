// <copyright company="SIX Networks GmbH" file="RecentMission.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Play.Core.Games.Legacy.Missions;

namespace withSIX.Play.Core.Options.Entries
{
    [DataContract(Name = "RecentMission",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class RecentMission : RecentBase<MissionBase>
    {
        public RecentMission(MissionBase obj) : base(obj) {}
    }
}