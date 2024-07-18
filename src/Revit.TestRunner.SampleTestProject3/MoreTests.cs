using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using NUnit.Framework;

namespace Revit.TestRunner.SampleTestProject
{
    public class MoreTests
    {
        [SetUp]
        public void SetUp( UIApplication uiApplication )
        {
            Assert.NotNull( uiApplication );
        }

        [TearDown]
        public void TearDown( UIApplication application )
        {
            Assert.NotNull( application );
        }

        [Test]
        public void Test2()
        {
            Delay( 2 );
        }

        [Test]
        public void Test4()
        {
            Delay( 2 );
            Delay( 2 );
        }

        [Test]
        public void Test5()
        {
            Delay( 5 );
        }

        [Test]
        public void Test5Fail()
        {
            Delay( 5 );
            Assert.Fail( "This test should fail!" );
        }

        [Test]
        public async Task Test3Async()
        {
            await DelayAsync( 3 );
        }

        [Test]
        public async Task Test3AsyncFail()
        {
            await DelayAsync( 3 );
            Assert.Fail( "This test should fail!" );
        }

        [Test, Explicit]
        public void Test6Explicit()
        {
            Delay( 2 );
        }

        [Test, Ignore( "Ignore Test" )]
        public void TestIgnore()
        {
            Delay( 2 );
        }

        private async Task DelayAsync( int aSeconds )
        {
            await Task.Delay( aSeconds * 1000 );
        }

        private void Delay( int aSeconds )
        {
            Thread.Sleep( aSeconds * 1000 );
        }
    }
}
