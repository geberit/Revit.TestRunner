using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;

namespace Revit.TestRunner.SampleTestProject
{
    public class ParameterTest
    {
        private Application mApplication;
        private UIApplication mUiApplication;

        [SetUp]
        public void SetUp( Application application, UIApplication uiApplication )
        {
            Assert.NotNull( uiApplication );
            Assert.NotNull( application );

            mApplication = application;
            mUiApplication = uiApplication;
        }

        [TearDown]
        public void TearDown( Application application, UIApplication uiApplication )
        {
            Assert.NotNull( uiApplication );
            Assert.NotNull( application );
        }

        [Test]
        public void SomeTestWithApplication()
        {
            Assert.NotNull( mUiApplication );
            Assert.NotNull( mApplication );
        }

        [TestCase( 12, 3, ExpectedResult = 15 )]
        [TestCase( 13, 7, ExpectedResult = 20 )]
        [TestCase( 15, 4, ExpectedResult = 19 )]
        public int SumTest( int n, int d )
        {
            return n + d;
        }
    }
}