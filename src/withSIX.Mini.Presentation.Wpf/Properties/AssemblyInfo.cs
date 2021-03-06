﻿// <copyright company="SIX Networks GmbH" file="AssemblyInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using withSIX.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Sync")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIX Networks GmbH")]

#if MAIN_RELEASE
[assembly: AssemblyProduct("Sync")]
#else
#if BETA_RELEASE
[assembly: AssemblyProduct("Sync BETA")]
#else
#if NIGHTLY_RELEASE
[assembly: AssemblyProduct("Sync ALPHA")]
#else

[assembly: AssemblyProduct("Sync DEV")]
#endif
#endif
#endif

[assembly: AssemblyMetadata("SquirrelAwareVersion", "1")]
[assembly: AssemblyCopyright("Copyright SIX Networks GmbH © 2015-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
//[assembly: AssemblyMetadata("SquirrelAwareVersion", "1")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
               ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
               //(used if a resource is not found in the page, 
               // or application resource dictionaries)
               ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
               //(used if a resource is not found in the page, 
               // app, or any theme specific resource dictionaries)
           )]


// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion(BuildFlags.Version)]
[assembly: AssemblyFileVersion(BuildFlags.Version)]
[assembly: AssemblyInformationalVersion(BuildFlags.ProductVersion)]
[assembly: InternalsVisibleTo("ImpromptuInterfaceDynamicAssembly")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Castle.Core")]
[assembly: InternalsVisibleTo("ShortBus")] // required for internal handlers like  the decorators...