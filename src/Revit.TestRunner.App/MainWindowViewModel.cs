using Revit.TestRunner.App.View;
using Revit.TestRunner.Shared;

namespace Revit.TestRunner.App
{
    /// <summary>
    /// ViewModel of the App Window.
    /// </summary>
    public class MainWindowViewModel : AbstractNotifyPropertyChanged
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
