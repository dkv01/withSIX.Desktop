// <copyright company="SIX Networks GmbH" file="XmlTools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.IO;
using System.Runtime.Serialization;
using System.Xml.Linq;
using NDepend.Path;
using System.Xml.Serialization;

namespace withSIX.Core.Applications.Extensions
{
    public static class XmlExt
    {
        public static T LoadXml<T>(this IAbsoluteFilePath src)
            => new XmlTools().LoadXmlFromFile<T>(src.ToString());
    }

    public class XmlTools
    {
        public T LoadXmlFromFile<T>(string path) {
            var serializer = new DataContractSerializer(typeof(T));
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return (T) serializer.ReadObject(fs);
        }

        public virtual T Deserialize<T>(XElement doc) {
            // if (doc == null) throw new ArgumentNullException(nameof(doc));

            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var reader = doc.CreateReader())
                return (T) xmlSerializer.Deserialize(reader);
        }

        public virtual void SaveXmlToDiskThroughMemory(object graph, IAbsoluteFilePath filePath, bool pretty = false) {
            // if (graph == null) throw new ArgumentNullException(nameof(graph));
            // if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            Tools.FileUtil.Ops.AddIORetryDialog(() => Tools.FileTools.SafeIO.SafeSave(x => {
                using (var ms = new MemoryStream()) {
                    var serializer = new DataContractSerializer(graph.GetType());
                    serializer.WriteObject(ms, graph);
                    ms.Seek(0, 0);
                    using (var fs = new FileStream(x.ToString(), FileMode.Create)) {
                        ms.CopyTo(fs);
                        fs.Flush(true);
                    }
                }
            }, filePath), filePath.ToString());
        }
    }
}