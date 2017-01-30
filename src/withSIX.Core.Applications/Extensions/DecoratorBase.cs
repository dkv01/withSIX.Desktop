// <copyright company="SIX Networks GmbH" file="DecoratorBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Applications.Extensions
{
    public abstract class DecoratorBase<T> where T : class
    {
        protected DecoratorBase(T decorated) {
            // if (decorated == null) throw new ArgumentNullException(nameof(decorated));

            Decorated = decorated;
        }

        protected T Decorated { get; }
    }
}