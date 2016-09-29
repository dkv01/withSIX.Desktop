// <copyright company="SIX Networks GmbH" file="CarrierCommandGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using withSIX.Api.Models.Games;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Attributes;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [Game(GameIds.CarrierCommand, Name = "Carrier Command: Gaea Mission", Slug = "Carrier-Command",
         Executables = new[] {"carrier.exe"})]
    [DataContract]
    public class CarrierCommandGame : BasicGame
    {
        protected CarrierCommandGame(Guid id) : this(id, new CarrierCommandGameSettings()) {}
        public CarrierCommandGame(Guid id, CarrierCommandGameSettings settings) : base(id, settings) {}
    }
}