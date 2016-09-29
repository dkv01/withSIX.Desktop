// <copyright company="SIX Networks GmbH" file="ConsoleFirstTimeLicense.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Presentation
{
    public class ConsoleFirstTimeLicense : IFirstTimeLicense
    {
        public bool ConfirmLicense(object obj) => true;
    }
}