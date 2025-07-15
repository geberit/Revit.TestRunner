using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClosedXML.Excel;
using log4net;
using Revit.TestRunner.Commands;
using Revit.TestRunner.Server;

namespace Revit.TestRunner.Addin
{
    /// <summary>
    /// Entreance object for the Revit.TestRunner addin in Revit.
    /// </summary>
    public class Main : IExternalApplication
    {
        private RunnerController mController;

        public Result OnStartup( UIControlledApplication application )
        {
            try {
                var dir = @"C:\temp\PluginShizzle";

                if( !Directory.Exists( dir ) ) Directory.CreateDirectory( dir );
                GlobalContext.Properties["TestRunnerPluginLogName"] = Path.Combine( dir, "LogTestRunner.txt" );

                Log.Info( $"Revit.TestRunner started v{Assembly.GetExecutingAssembly().GetName().Version} '{DateTime.Now}'" );
                Log.Info( $"{Environment.OSVersion}, NetFX {Environment.Version}" );
                Log.Debug( $"Log Directory '{Log.LogDirectory}'" );
                Log.Debug( $"CurrentAppDomain.ApplicationBase '{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}'" );

                RibbonPanel ribbonPanel = application.CreateRibbonPanel( "Testing" );

                string command = typeof( RunnerCommand ).FullName;

                XLWorkbook workbook = new XLWorkbook();
                workbook.Worksheets.Add( "Test" );
                workbook.SaveAs( Path.Combine( dir, "test.xlsx" ) );

                PushButtonData buttonData = new PushButtonData( command, "Open Runner", Assembly.GetExecutingAssembly().Location, command ) {
                    ToolTip = "Open the Test Runner Dialog\nStart Tests using the Revit API.",
                    Image = GetImage( "Testing.png", 16 ),
                    LargeImage = GetImage( "Testing.png", 32 ),
                    AvailabilityClassName = typeof( AvailableInStartScreen ).FullName
                };

                ribbonPanel.AddItem( buttonData );

                mController = new RunnerController();
                mController.Start( application );
            }
            catch( Exception ex ) {
                TaskDialog.Show( "Revit.TestRunner", $"Error during startup: {ex}" );
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public Result OnShutdown( UIControlledApplication application )
        {
            mController.Stop();
            return Result.Succeeded;
        }

        private static ImageSource GetImage( string name, int size )
        {
            ImageSource result = null;

            try {
                string ressource = "pack://application:,,,/Revit.TestRunner;component/Commands/Pic/" + name;
                result = new BitmapImage( new Uri( ressource ) );
                result = Thumbnail( result, size );
            }
            catch {
                // ignore
            }

            return result;
        }

        private static ImageSource Thumbnail( ImageSource source, int size )
        {
            Rect rect = new Rect( 0, 0, size, size );
            DrawingVisual drawingVisual = new DrawingVisual();
            using( DrawingContext drawingContext = drawingVisual.RenderOpen() ) {
                drawingContext.DrawImage( source, rect );
            }

            RenderTargetBitmap resizedImage = new RenderTargetBitmap( (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default );
            resizedImage.Render( drawingVisual );

            return resizedImage;
        }
    }
}