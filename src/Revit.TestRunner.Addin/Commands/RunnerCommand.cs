using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit.TestRunner.Commands
{
    /// <summary>
    /// Command in Addin section of Revit.
    /// Call the standalone app.exe if available.
    /// </summary>
    [Transaction( TransactionMode.Manual )]
    public class RunnerCommand : IExternalCommand
    {
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            FileInfo file = new FileInfo( Assembly.GetExecutingAssembly().Location );
            var exe = Path.Combine( file.Directory.FullName, @"..\Client\Revit.TestRunner.App.exe" );

            if( File.Exists( exe ) ) {
                Process.Start( exe );
            }
            else {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                MessageBox.Show( "Please use Revit.TestRunner Standalone App.", $"Revit.TestRunner {version}", MessageBoxButton.OK, MessageBoxImage.Information );
            }

            return Result.Succeeded;
        }
    }
}