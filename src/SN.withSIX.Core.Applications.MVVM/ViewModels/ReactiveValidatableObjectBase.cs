// <copyright company="SIX Networks GmbH" file="ReactiveValidatableObjectBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Caliburn.Micro;
using withSIX.Core.Logging;

namespace withSIX.Core.Applications.MVVM.ViewModels
{
    [DataContract]
    public abstract class ReactiveValidatableObjectBase : ReactiveValidatableObject, INotifyPropertyChangedEx,
        IEnableLogging
    {
        public bool IsNotifying
        {
            get { return AreChangeNotificationsEnabled(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        ///     Notifies subscribers of the property change.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public virtual void NotifyOfPropertyChange(string propertyName) {
            OnPropertyChanged(propertyName);
        }

        public void Refresh() {
            NotifyOfPropertyChange(string.Empty);
        }

        /// <summary>
        ///     Notifies subscribers of the property change.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="property">The property expression.</param>
        public virtual void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property) {
            NotifyOfPropertyChange(property.GetMemberInfo().Name);
        }

        protected override bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            OnPropertyChanging(propertyName);
            storage = value;
            OnPropertyChanged(propertyName);
            if (Validator != null)
                UpdateValidation();
            return true;
        }

        public virtual bool ShouldSerializeIsNotifying() => false;
    }
}