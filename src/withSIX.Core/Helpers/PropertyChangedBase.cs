// <copyright company="SIX Networks GmbH" file="PropertyChangedBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using withSIX.Core.Logging;

namespace withSIX.Core.Helpers
{
    [DataContract]
    public abstract class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null) {
                var propertyChangedEventArgs = new PropertyChangedEventArgs(propertyName);
                handler(this, propertyChangedEventArgs);
            }
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Workaround: https://social.msdn.microsoft.com/Forums/vstudio/en-US/c69b3837-a296-4232-9916-89ed7be3fb59/nullreferenceexception-at-msinternaldataclrbindingworkeronsourcepropertychangedobject-o-string?forum=csharpgeneral
        protected virtual bool SetPropertySafe<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) {
            try {
                return SetProperty(ref storage, value, propertyName);
            } catch (NullReferenceException ex) {
                if (!ex.StackTrace.Contains(
                    "MS.Internal.Data.ClrBindingWorker.OnSourcePropertyChanged(Object o, String propName)"))
                    throw;
                MainLog.Logger.Warn("NullReferenceException trying to SetProperty, probably Framework error!");
            }
            return true;
        }

        public void Refresh() {
            OnPropertyChanged("");
        }
    }
}