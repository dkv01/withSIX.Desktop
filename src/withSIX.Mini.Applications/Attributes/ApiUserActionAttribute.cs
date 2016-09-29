// <copyright company="SIX Networks GmbH" file="ApiUserActionAttribute.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Mini.Applications.Attributes
{
    public class ApiUserActionAttribute : Attribute
    {
        public ApiUserActionAttribute(string nameOverride = null) {
            NameOverride = nameOverride;
        }

        /// <summary>
        ///     Verb
        /// </summary>
        public string NameOverride { get; }
    }
}