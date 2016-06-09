// <copyright company="SIX Networks GmbH" file="Country.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

public class Country
{
    readonly string code;
    readonly string name;

    /**
     * Creates a new Country.
     *
     * @param code the country code.
     * @param name the country name.
     */

    public Country(string code, string name) {
        this.code = code;
        this.name = name;
    }

    /**
     * Returns the ISO two-letter country code of this country.
     *
     * @return the country code.
     */

    public string getCode() => code;

    /**
     * Returns the name of this country.
     *
     * @return the country name.
     */

    public string getName() => name;
}