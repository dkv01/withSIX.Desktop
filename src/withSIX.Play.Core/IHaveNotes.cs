// <copyright company="SIX Networks GmbH" file="IHaveNotes.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Play.Core
{
    public interface IHaveNotes
    {
        string NoteName { get; }
        bool HasNotes { get; }
        string Notes { get; set; }
    }
}