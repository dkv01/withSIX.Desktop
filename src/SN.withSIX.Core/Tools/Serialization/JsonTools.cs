﻿// <copyright company="SIX Networks GmbH" file="JsonTools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using NDepend.Path;
using Newtonsoft.Json;
using SN.withSIX.Core.Extensions;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Core
{
    public partial class Tools
    {
        public class JsonTools
        {
            public T LoadJsonFromFile<T>(IAbsoluteFilePath file) => LoadTextFromFile(file).FromJson<T>();

            public T LoadJsonFromFile<T>(IAbsoluteFilePath file, JsonSerializerSettings settings)
                => LoadTextFromFile(file).FromJson<T>(settings);

            public async Task<T> LoadJsonFromFileAsync<T>(IAbsoluteFilePath file)
                => (await LoadTextFromFileAsync(file).ConfigureAwait(false)).FromJson<T>();

            public async Task<T> LoadJsonFromFileAsync<T>(IAbsoluteFilePath file, JsonSerializerSettings settings)
                => (await LoadTextFromFileAsync(file).ConfigureAwait(false)).FromJson<T>(settings);


            public string LoadTextFromFile(IAbsoluteFilePath path) {
                Contract.Requires<ArgumentNullException>(path != null);

                var text = FileUtil.Ops.ReadTextFile(path);
                return string.IsNullOrWhiteSpace(text)
                    ? null
                    : text;
            }

            public async Task<string> LoadTextFromFileAsync(IAbsoluteFilePath path) {
                Contract.Requires<ArgumentNullException>(path != null);

                var text = await FileUtil.Ops.ReadTextFileAsync(path).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(text)
                    ? null
                    : text;
            }

            public void SaveJsonToDiskThroughMemory(object graph, IAbsoluteFilePath filePath, bool pretty = false) {
                Contract.Requires<ArgumentNullException>(graph != null);
                Contract.Requires<ArgumentNullException>(filePath != null);
                SaveJsonToDiskThroughMemory(graph, filePath, JsonSupport.DefaultSettings, pretty);
            }

            void SaveJsonToDiskThroughMemory(object graph, IAbsoluteFilePath filePath,
                JsonSerializerSettings settings, bool pretty = false) {
                var json = graph.ToJson(settings, pretty);
                FileTools.SafeIO.SafeSave(x => SaveTextToFile(x, json), filePath);
            }

            static void SaveTextToFile(IAbsoluteFilePath x, string json) {
                FileUtil.Ops.CreateText(x, json);
            }
        }
    }
}