// <copyright company="SIX Networks GmbH" file="ObjectSaveBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace withSIX.Play.Core.Options.Entries
{
    [DataContract]
    public abstract class ObjectSaveBase<T> where T : class, IObjectTag
    {
        [DataMember] readonly string _key;

        protected ObjectSaveBase(T obj) {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (!(obj.ObjectTag != null)) throw new ArgumentNullException("obj.ObjectTag != null");
            _key = obj.ObjectTag;
        }

        internal string Key => _key;
        // public T Obj { get; set; }

        public bool Matches(T obj) => obj != null && _key.Equals(obj.ObjectTag, StringComparison.CurrentCultureIgnoreCase);
    }
}