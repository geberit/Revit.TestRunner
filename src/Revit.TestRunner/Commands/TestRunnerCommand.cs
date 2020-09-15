using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit.TestRunner.Commands
{
    [Transaction( TransactionMode.Manual )]
    public class TestRunnerCommand : IExternalCommand
    {
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            FileInfo file = new FileInfo( Assembly.GetExecutingAssembly().Location );
            var exe = Path.Combine( file.Directory.FullName, "Revit.TestRunner.App.exe" );

            if( File.Exists( exe ) ) {
                Process.Start( exe );
            }
            else {
                MessageBox.Show( "Please use Revit.TestRunner Standalone App.", "Revit.TestRunner", MessageBoxButton.OK, MessageBoxImage.Information );
            }

            return Result.Succeeded;
        }
    }
}