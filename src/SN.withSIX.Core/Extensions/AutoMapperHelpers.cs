// <copyright company="SIX Networks GmbH" file="AutoMapperHelpers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using AutoMapper;
using NDepend.Path;
using withSIX.Api.Models;

namespace SN.withSIX.Core.Extensions
{
    public static class AutoMapperHelpers
    {
        public static void SetupConverters(this IProfileExpression config) {
            config.SetupUriConverter();
            config.SetupDirConverter();
            config.SetupVersionConverter();
            config.SetupServerAddressConverter();
        }

        static void SetupUriConverter(this IProfileExpression config) {
            config.CreateMap<Uri, string>().ConvertUsing<UriToStringConverter>();
            config.CreateMap<string, Uri>().ConvertUsing<StringToUriConverter>();
        }

        static void SetupServerAddressConverter(this IProfileExpression config) {
            //config.CreateMap<ServerAddress, string>().ConvertUsing<ServerAddressToStringConverter>();
            //config.CreateMap<string, ServerAddress>().ConvertUsing<StringToServerAddressConverter>();
        }

        static void SetupVersionConverter(this IProfileExpression config) {
            config.CreateMap<Version, string>().ConvertUsing<VersionToStringConverter>();
            config.CreateMap<string, Version>().ConvertUsing<StringToVersionConverter>();
        }

        static void SetupDirConverter(this IProfileExpression config) {
            config.CreateMap<IAbsoluteDirectoryPath, string>().ConvertUsing<AbsoluteDirToStringConverter>();
            config.CreateMap<string, IAbsoluteDirectoryPath>().ConvertUsing<StringToAbsoluteDirConverter>();
            config.CreateMap<IAbsoluteFilePath, string>().ConvertUsing<AbsoluteFileToStringConverter>();
            config.CreateMap<string, IAbsoluteFilePath>().ConvertUsing<StringToAbsoluteFileConverter>();
            config.CreateMap<Guid, ShortGuid>().ConvertUsing<GuidToShortGuidConverter>();
            config.CreateMap<ShortGuid, Guid>().ConvertUsing<ShortGuidToGuidConverter>();
        }

        class GuidToShortGuidConverter : ITypeConverter<Guid, ShortGuid>
        {
            public ShortGuid Convert(Guid source, ShortGuid destination, ResolutionContext context) => new ShortGuid(source);
        }

        class ShortGuidToGuidConverter : ITypeConverter<ShortGuid, Guid>
        {
            public Guid Convert(ShortGuid source, Guid destination, ResolutionContext context) => source?.ToGuid() ?? Guid.Empty;
        }

        class AbsoluteDirToStringConverter : ITypeConverter<IAbsoluteDirectoryPath, string>
        {
            public string Convert(IAbsoluteDirectoryPath source, string destination, ResolutionContext context) => source?.ToString();
        }

        class AbsoluteFileToStringConverter : ITypeConverter<IAbsoluteFilePath, string>
        {
            public string Convert(IAbsoluteFilePath source, string destination, ResolutionContext context) => source?.ToString();

        }

        class StringToAbsoluteDirConverter : ITypeConverter<string, IAbsoluteDirectoryPath>
        {
            public IAbsoluteDirectoryPath Convert(string source, IAbsoluteDirectoryPath destination, ResolutionContext context) => source?.ToAbsoluteDirectoryPath();
        }

        class StringToAbsoluteFileConverter : ITypeConverter<string, IAbsoluteFilePath>
        {
            public IAbsoluteFilePath Convert(string source, IAbsoluteFilePath destination, ResolutionContext context) => source?.ToAbsoluteFilePath();
        }

        class StringToUriConverter : ITypeConverter<string, Uri>
        {
            public Uri Convert(string source, Uri destination, ResolutionContext context) => source == null ? null : new Uri(source);
        }
/*
        class StringToServerAddressConverter : ITypeConverter<string, ServerAddress>
        {

            public ServerAddress Convert(string source, ServerAddress destination, ResolutionContext context) => source == null ? null : new ServerAddress(source);
        }
        */
        class StringToVersionConverter : ITypeConverter<string, Version>
        {
            public Version Convert(string source, Version destination, ResolutionContext context) => source == null ? null : Version.Parse(source);
        }

        class UriToStringConverter : ITypeConverter<Uri, string>
        {
            public string Convert(Uri source, string destination, ResolutionContext context) => source?.AbsoluteUri;
        }

        /*
        class ServerAddressToStringConverter : ITypeConverter<ServerAddress, string>
        {
            public string Convert(ServerAddress source, string destination, ResolutionContext context) => source?.ToString();
        }
        */

        class VersionToStringConverter : ITypeConverter<Version, string>
        {
            public string Convert(Version source, string destination, ResolutionContext context) => source?.ToString();
        }
    }
}