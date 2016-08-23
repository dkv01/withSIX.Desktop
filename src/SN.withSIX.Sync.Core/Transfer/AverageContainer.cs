// <copyright company="SIX Networks GmbH" file="AverageContainer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace SN.withSIX.Sync.Core.Transfer
{
    public class AverageContainer
    {
        private readonly int _count;
        protected List<long> _speeds { get; } = new List<long>();

        public AverageContainer(int count) {
            _count = count;
        }

        public long Update(long speed) {
            lock (_speeds) {
                _speeds.Add(speed);
                if (_speeds.Count > _count)
                    _speeds.RemoveAt(0);
                return Convert.ToInt64(_speeds.Average());
            }
        }
    }

    public class AverageContainer2 : AverageContainer
    {
        bool _isZero = true;
        public AverageContainer2(int count) : base(count) {}

        public long? UpdateSpecial(long? speed) {
            lock (_speeds) {
                long? newSpeed = null;
                if (_isZero) {
                    if (speed.HasValue && speed.Value > 0) {
                        _isZero = false;
                        newSpeed = Update(speed.Value);
                    }
                } else {
                    newSpeed = Update(speed.GetValueOrDefault(0));
                    if (newSpeed <= 0) {
                        _isZero = true;
                        _speeds.Clear();
                    }
                }
                return _isZero ? null : newSpeed;
            }
        }
    }
}