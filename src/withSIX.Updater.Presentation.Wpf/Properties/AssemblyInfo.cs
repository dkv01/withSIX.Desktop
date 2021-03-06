﻿// <copyright company="SIX Networks GmbH" file="AssemblyInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("withSIX Updater")]
[assembly: AssemblyDescription("SIX Updater Presentation")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIX Networks")]
#if BETA_RELEASE
[assembly: AssemblyProduct("SIX Updater BETA")]
#else
#if NIGHTLY_RELEASE
[assembly: AssemblyProduct("SIX Updater ALPHA")]
#else

[assembly: AssemblyProduct("SIX Updater")]
#endif
#endif

[assembly: AssemblyCopyright("Copyright © SIX Networks 2009-2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

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


// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.5.*")]

[assembly: AssemblyVersion("1.51.0.666")]
[assembly: AssemblyFileVersion("1.51.0.666")]
[assembly: AssemblyInformationalVersion("1.51.0-dev201501241")]
[assembly: InternalsVisibleTo("withSIX.Play.Tests.Presentation")]
[assembly: InternalsVisibleTo("ShortBus")] // required for internal handlers like the decorators...