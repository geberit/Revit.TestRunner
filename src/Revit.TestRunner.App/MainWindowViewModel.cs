using Revit.TestRunner.App.View;

namespace Revit.TestRunner.App
{
    /// <summary>
    /// ViewModel of the App Window.
    /// </summary>
    public class MainWindowViewModel : AbstractViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindowViewModel()
        {
            Overview = new OverviewViewModel();
        }

        /// <summary>
        /// Get <see cref="OverviewView"/> DataContext.
        /// </summary>
        public OverviewViewModel Overview { get; }
    }
}
