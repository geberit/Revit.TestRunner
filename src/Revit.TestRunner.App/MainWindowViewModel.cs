using Revit.TestRunner.App.View;

namespace Revit.TestRunner.App
{
    public class MainWindowViewModel : AbstractViewModel
    {

        public MainWindowViewModel()
        {
            Overview = new OverviewViewModel();
        }

        public OverviewViewModel Overview { get; }
    }
}
