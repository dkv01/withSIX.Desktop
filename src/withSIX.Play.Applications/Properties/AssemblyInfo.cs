﻿// <copyright company="SIX Networks GmbH" file="AssemblyInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Six.Play")]
[assembly: AssemblyDescription("SIX Play Domain and Application layer")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIX Networks")]
[assembly: AssemblyProduct("SIX Play library")]
[assembly: AssemblyCopyright("Copyright © SIX Networks 2009-2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("5c6368c4-20c3-4c71-966a-29e4a5e2cad3")]

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
[assembly: InternalsVisibleTo("withSIX.Play.Tests.Presentation")]
[assembly: InternalsVisibleTo("ShortBus")] // required for internal handlers