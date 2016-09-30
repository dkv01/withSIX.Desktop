// <copyright company="SIX Networks GmbH" file="Userconfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.ComponentModel;
using System.Runtime.Serialization;
using withSIX.Core.Helpers;

namespace withSIX.Play.Core.Games.Legacy.Arma
{
    public abstract class Userconfig : PropertyChangedBase
    {
        [IgnoreDataMember]
        [Browsable(false)]
        public string Name => GetType().Name;
        public abstract bool Save();
    }
}