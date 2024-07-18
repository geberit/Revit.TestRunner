using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle( "Revit.TestRunner" )]

[assembly: Guid( "6930154f-9b8f-4650-817c-a9cceb4f7075" )]
[assembly: ComVisible( false )]



[assembly: AssemblyCompany( "Geberit International AG" )]
[assembly: AssemblyProduct( "Revit TestRunner" )]
[assembly: AssemblyCopyright( "Copyright © 2002-2024, Geberit International AG." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

[assembly: NeutralResourcesLanguage( "en-US" )]
[assembly: AssemblyDescription( "UnitTest Runner for Revit" )]

#if DEBUG

[assembly: AssemblyConfiguration( "Debug Build" )]
#else

[assembly: AssemblyConfiguration( "Release Build" )]
#endif



[assembly: AssemblyVersion( "1.4.1.0" )]
[assembly: AssemblyFileVersion( "1.4.1.0" )]

[assembly: AssemblyInformationalVersion( "1.4.1.0" )]

