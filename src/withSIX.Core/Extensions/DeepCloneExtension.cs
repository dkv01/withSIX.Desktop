// <copyright company="SIX Networks GmbH" file="DeepCloneExtension.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Extensions
{
    public static class DeepCloneExtension
    {
        /*
        public static T DeepClone<T>(T source) where T : class {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using (var ms = new MemoryStream()) {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, source);
                ms.Position = 0;
                return (T) formatter.Deserialize(ms);
            }
        }
        */
    }
}