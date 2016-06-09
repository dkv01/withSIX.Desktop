// <copyright company="SIX Networks GmbH" file="Region.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

public class Region
{
    public string countryCode;
    public string countryName;
    public string region;

    public Region() {}

    public Region(string countryCode, string countryName, string region) {
        this.countryCode = countryCode;
        this.countryName = countryName;
        this.region = region;
    }

    public string getcountryCode() => countryCode;

    public string getcountryName() => countryName;

    public string getregion() => region;
}