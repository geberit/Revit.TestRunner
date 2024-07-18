using System.Reflection;
using System.Resources;

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
