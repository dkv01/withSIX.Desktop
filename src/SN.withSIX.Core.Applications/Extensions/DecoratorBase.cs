// <copyright company="SIX Networks GmbH" file="DecoratorBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Applications.Extensions
{
    public abstract class DecoratorBase<T> where T : class
    {
        protected DecoratorBase(T decorated) {
            // Contract.Requires<ArgumentNullException>(decorated != null);

            Decorated = decorated;
        }

        protected T Decorated { get; }
    }
}