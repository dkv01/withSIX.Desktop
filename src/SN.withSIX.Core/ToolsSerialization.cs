// <copyright company="SIX Networks GmbH" file="Serialization.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core
{
    public static partial class Tools
    {
        public static SerializationTools Serialization = new SerializationTools();

        public class SerializationTools
        {
            public JsonTools Json = new JsonTools();
        }
    }
}