using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Revit.TestRunner.Shared
{
    public static class RevitHelper
    {
        private const string Revit = "Autodesk Revit";

        /// <summary>
        /// Get a list of installed Revit applications.
        /// </summary>
        public static IEnumerable<string> GetInstalledRevitApplications()
        {
            var installedPrograms = GetInstalledApplications();
            var revitInstallers = installedPrograms.Where( s => s.Contains( Revit ) );
            var revitInstalled = revitInstallers
                .Where( s => int.TryParse( s.Substring( s.Length - 4 ), out int i ) )
                .Where( s => s.StartsWith( Revit ) )
                .Where( s => s.Length == Revit.Length + 5 )
                .Distinct();


            return revitInstalled.OrderBy( s => s );
        }

        /// <summary>
        /// Get a list of installed applications.
        /// </summary>
        private static IEnumerable<string> GetInstalledApplications()
        {
            var result = new List<string>();
            const string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using( RegistryKey rk = Registry.LocalMachine.OpenSubKey( uninstallKey ) ) {
                foreach( string skName in rk.GetSubKeyNames() ) {
                    using( RegistryKey sk = rk.OpenSubKey( skName ) ) {
                        try {
                            var obj = sk.GetValue( "DisplayName" );

                            if( obj != null ) {
                                result.Add( obj.ToString() );
                            }
                        }
                        catch( Exception ) {
                            // ignored
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Start of Revit in <paramref name="version"/>.
        /// If any Revit already run, no more Revit will be started.
        /// Returns the process id and if it was new started.
        /// </summary>
        public static (int ProcessId, bool IsNew) StartRevit( string version, string language = "" )
        {
            Process process = null;
            bool isNew = false;

            var processes = Process.GetProcessesByName( "Revit" );

            if( processes.Length == 0 ) {
                var programFiles = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles );
                var executablePath = Path.Combine( programFiles, "Autodesk", $"Revit {version}", "Revit.exe" );

                var argument = !string.IsNullOrEmpty( language )
                    ? $" /language {language}"
                    : string.Empty;

                if( File.Exists( executablePath ) ) {
                    Console.WriteLine( $"Starting Revit {version}" );

                    process = Process.Start( executablePath, argument );
                    isNew = true;
                }
            }
            else if( processes.Length == 1 ) {
                process = processes.Single();
                //Console.WriteLine($"Using Revit ProcessId[{process.Id}]");
            }
            else {
                throw new ApplicationException( "Too many Revit applications already running! Max 1 allowed." );
            }

            return (process.Id, isNew);
        }

        public static void KillRevit( int processId )
        {
            //Console.WriteLine($"Killing Revit");
            var process = Process.GetProcessById( processId );
            process?.Kill();
        }
    }
}