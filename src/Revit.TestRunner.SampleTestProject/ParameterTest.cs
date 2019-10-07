using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using NUnit.Framework;

namespace Revit.TestRunner.SampleTestProject
{
    public class ParameterTest
    {
        [SetUp]
        public void SetUp( UIApplication uiApplication )
        {
            Assert.NotNull( uiApplication );
        }

        [TearDown]
        public void TearDown( Application application )
        {
            Assert.NotNull( application );
        }


        [Test]
        public void UiApplicationTest( UIApplication uiApplication )
        {
            Assert.NotNull( uiApplication );
        }

        [Test]
        public void ApplicationTest( Application application )
        {
            Assert.IsNotNull( application );
        }

        [Test]
        public void MultiParameterTest1( UIApplication uiApplication, Application application )
        {
            Assert.IsNotNull( uiApplication.Application );
            Assert.IsNotNull( application );
        }

        [Test]
        public void MultiParameterTest2( Application application, UIApplication uiApplication )
        {
            Assert.IsNotNull( uiApplication );
            Assert.IsNotNull( application );
        }
    }
}
