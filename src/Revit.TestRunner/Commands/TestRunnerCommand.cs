using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.TestRunner.View;

namespace Revit.TestRunner.Commands
{
    [Transaction( TransactionMode.Manual )]
    public class TestRunnerCommand : IExternalCommand
    {
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            NUnitRunnerViewModel viewModel = new NUnitRunnerViewModel( commandData.Application );
            DialogWindow.Show<NUnitRunnerView>( viewModel );

            return Result.Succeeded;
        }
    }
}