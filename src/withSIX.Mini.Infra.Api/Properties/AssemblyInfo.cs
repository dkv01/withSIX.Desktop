﻿// <copyright company="SIX Networks GmbH" file="AssemblyInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using withSIX.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("withSIX.Mini.Infra.Api")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("withSIX.Mini.Infra.Api")]
[assembly: AssemblyCopyright("Copyright © 2015-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("80c8f542-5bc1-4bbe-9c5a-0e0ca244a3f0")]

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
[assembly: InternalsVisibleTo("ImpromptuInterfaceDynamicAssembly")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Castle.Core")]
[assembly: InternalsVisibleTo("ShortBus")] // required for internal handlers like the decorators...