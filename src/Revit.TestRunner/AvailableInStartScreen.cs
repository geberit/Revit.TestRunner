using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit.TestRunner
{
    /// <summary>
    /// External Command Visability: No document open.
    /// </summary>
    public class AvailableInStartScreen : IExternalCommandAvailability
    {
        public bool IsCommandAvailable( UIApplication applicationData, CategorySet selectedCategories )
        {
            return applicationData.ActiveUIDocument == null;
        }
    }
}
