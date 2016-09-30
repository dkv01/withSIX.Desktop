// <copyright company="SIX Networks GmbH" file="ISafeCall.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Core.Services
{
    public interface ISafeCall
    {
        void Do(Action act);
        TResult Do<TResult>(Func<TResult> action);
    }

    public interface ISafeCallFactory
    {
        ISafeCall Create();
    }

    public abstract class LockedWrapper
    {
        public static ISafeCallFactory callFactory { get; set; }
    }
}