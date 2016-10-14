﻿// <copyright company="SIX Networks GmbH" file="Bridge.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
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
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Binder = new NamespaceMigrationSerializationBinder(),
            Error = OnError,
        }.SetDefaultConverters();

        JsonSerializerSettings IBridge.OtherSettings() {
            return OtherSettings();
        }

        // TODO: Consider implications for the actual S-IR system messages: http://stackoverflow.com/questions/37832165/signalr-net-core-camelcase-json-contract-resolver/39410434#39410434
        public static JsonSerializerSettings OtherSettings() => new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            //TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
            // TODO: Very dangerous because we cant load/save when versions change?!? http://stackoverflow.com/questions/32245340/json-net-error-resolving-type-in-powershell-cmdlet
            //PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.Auto,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            //Binder = new NamespaceMigrationSerializationBinder(),
            Error = OnError,
        }.SetDefaultConverters();

        public class NamespaceMigrationSerializationBinder : DefaultSerializationBinder
        {
            //private readonly INamespaceMigration[] _migrations;

            public NamespaceMigrationSerializationBinder(/*params INamespaceMigration[] migrations*/) {
                //_migrations = migrations;
            }

            static readonly string searchStr = "SN.withSIX.";

            public override Type BindToType(string assemblyName, string typeName) {
                //var migration = _migrations.SingleOrDefault(p => p.FromAssembly == assemblyName && p.FromType == typeName);
                //if (migration != null) {
                    //return migration.ToType;
                //}
                if (assemblyName.StartsWith(searchStr))
                    assemblyName = assemblyName.Substring(3);
                if (typeName.StartsWith(searchStr))
                    typeName = typeName.Substring(3);
                return base.BindToType(assemblyName, typeName);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e) {
            //MainLog.Logger.Warn($"Error during JSON serialization for {e.CurrentObject}, {e.ErrorContext.Path} {e.ErrorContext.Member}: {e.ErrorContext.Error.Message}");
        }
    }
}