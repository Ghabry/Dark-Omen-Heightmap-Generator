using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle(ThisAssembly.Title)]
[assembly: AssemblyProduct(ThisAssembly.Product)]
[assembly: AssemblyDescription(ThisAssembly.Description)]
[assembly: AssemblyCopyright(ThisAssembly.Copyright)]
[assembly: AssemblyVersion(ThisAssembly.Version)]
[assembly: AssemblyInformationalVersionAttribute(ThisAssembly.InformationalVersion)]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
