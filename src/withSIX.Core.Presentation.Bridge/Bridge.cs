// <copyright company="SIX Networks GmbH" file="Bridge.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using withSIX.Api.Models.Extensions;

namespace withSIX.Core.Presentation.Bridge
{
    public class Bridge : IBridge, IPresentationService
    {
        // http://stackoverflow.com/questions/27080363/missingmethodexception-with-newtonsoft-json-when-using-typenameassemblyformat-wi

        public JsonSerializerSettings GameContextSettings() => new JsonSerializerSettings {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
            // TODO: Very dangerous because we cant load/save when versions change?!? http://stackoverflow.com/questions/32245340/json-net-error-resolving-type-in-powershell-cmdlet
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            Error = OnError,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        }.SetDefaultConverters();

        private void OnError(object sender, ErrorEventArgs e) {
            //MainLog.Logger.Warn($"Error during JSON serialization for {e.CurrentObject}, {e.ErrorContext.Path} {e.ErrorContext.Member}: {e.ErrorContext.Error.Message}");
        }
    }
}