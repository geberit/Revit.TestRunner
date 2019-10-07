using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit.TestRunner
{
    public class AvailableInStartScreen : IExternalCommandAvailability
    {
        public bool IsCommandAvailable( UIApplication applicationData, CategorySet selectedCategories )
        {
            return applicationData.ActiveUIDocument == null;
        }
    }
}
