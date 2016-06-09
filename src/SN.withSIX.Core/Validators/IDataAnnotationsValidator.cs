// <copyright company="SIX Networks GmbH" file="IDataAnnotationsValidator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAnnotationsValidator
{
    public interface IDataAnnotationsValidator
    {
        bool TryValidateObject(object obj, ICollection<ValidationResult> results);
        bool TryValidateObjectRecursive<T>(T obj, List<ValidationResult> results);
    }
}