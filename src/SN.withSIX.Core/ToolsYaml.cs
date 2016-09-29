// <copyright company="SIX Networks GmbH" file="YamlTools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Core
{
    public class YamlExpectedOtherNodeTypeException : Exception
    {
        public YamlExpectedOtherNodeTypeException(string message) : base(message) {}
    }


    public class YamlParseException : Exception
    {
        public YamlParseException(string message) : base(message) {}

        public YamlParseException(string message, Exception innerException) : base(message, innerException) {}
    }
}