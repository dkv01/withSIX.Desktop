// <copyright company="SIX Networks GmbH" file="ServerFilterBuilder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using SteamLayerWrap;

namespace withSIX.Steam.Plugin.Arma
{
    public class ServerFilterBuilder
    {
        private readonly ServerFilterWrap _filter;
        private bool _isFinal;

        public ServerFilterBuilder() {
            _filter = new ServerFilterWrap();
        }

        public ServerFilterWrap Value
        {
            get
            {
                _isFinal = true;
                return _filter;
            }
        }

        public static ServerFilterBuilder Build() => new ServerFilterBuilder();

        public ServerFilterBuilder FilterByAddresses(IReadOnlyCollection<IPEndPoint> list) {
            MakeFilterStep();
            AddOr(list, x => FilterByAddress(x));
            return this;
        }

        void AddOr<T>(IReadOnlyCollection<T> list, Action<T> act) {
            _filter.AddFilter("or", list.Count.ToString());
            foreach (var point in list)
                act(point);
        }

        public ServerFilterBuilder FilterByAddress(IPEndPoint point) {
            _filter.AddFilter("gameaddr", $"{point.Address}:{point.Port}");
            return this;
        }

        private void MakeFilterStep() {
            if (_isFinal)
                throw new NotSupportedException("Already obtained the value");
        }
    }
}