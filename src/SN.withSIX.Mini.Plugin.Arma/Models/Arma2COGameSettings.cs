// <copyright company="SIX Networks GmbH" file="Arma2COGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using NDepend.Path;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class Arma2COGameSettings : Arma2OaGameSettings
    {
        public Arma2COGameSettings() {
            StartupParameters = new Arma2COStartupParameters(DefaultStartupParameters);
        }

        [DataMember]
        public IAbsoluteDirectoryPath Arma2GameDirectory { get; set; }
        [DataMember, Obsolete("old workaround")]
        protected string Arma2GameDirectoryInternal { get; set; }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            if (Arma2GameDirectoryInternal != null) {
                Arma2GameDirectory = Arma2GameDirectoryInternal.ToAbsoluteDirectoryPath();
                Arma2GameDirectoryInternal = null;
            }
        }
    }
}