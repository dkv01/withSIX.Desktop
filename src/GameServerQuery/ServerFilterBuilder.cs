// <copyright company="SIX Networks GmbH" file="ServerFilterBuilder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;

namespace GameServerQuery
{
    public interface IServerFilterBuilder
    {
        ServerFilterBuilder FilterByAddresses(IReadOnlyCollection<IPEndPoint> list);
        ServerFilterBuilder FilterByAddress(IPEndPoint point);
    }

    public class ServerFilterBuilder : IServerFilterBuilder
    {
        private readonly List<Tuple<string, string>> _filter = new List<Tuple<string, string>>();
        private bool _isFinal;

        public List<Tuple<string, string>> Value
        {
            get
            {
                _isFinal = true;
                return _filter;
            }
        }

        public ServerFilterBuilder FilterByGame(string game) {
            AddFilter("gamedir", game);
            return this;
        }

        public ServerFilterBuilder FilterByAddresses(IReadOnlyCollection<IPEndPoint> list) {
            MakeFilterStep();
            AddOr(list, x => FilterByAddress(x));
            return this;
        }

        public ServerFilterBuilder FilterByAddress(IPEndPoint point) {
            AddFilter("gameaddr", $"{point.Address}:{point.Port}");
            return this;
        }

        void AddFilter(string key, string value) => _filter.Add(Tuple.Create(key, value));

        public static ServerFilterBuilder Build() => new ServerFilterBuilder();

        void AddOr<T>(IReadOnlyCollection<T> list, Action<T> act) {
            AddFilter("or", list.Count.ToString());
            foreach (var point in list)
                act(point);
        }

        public ServerFilterBuilder FilterByDedicated() {
            AddFilter("dedicated", "1");
            return this;
        }

        private void MakeFilterStep() {
            if (_isFinal)
                throw new NotSupportedException("Already obtained the value");
        }
    }
}