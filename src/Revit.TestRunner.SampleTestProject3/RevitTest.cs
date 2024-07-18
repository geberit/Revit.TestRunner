using Autodesk.Revit.DB;
using NUnit.Framework;

namespace Revit.TestRunner.SampleTestProject
{
    public class RevitTest
    {
        [Test]
        public void XyzTest()
        {
            XYZ xyz = new XYZ( 1, 2, 3 );
            XYZ test = xyz.Add( new XYZ( 5, 6, 7 ) );
            Assert.That( test.X == 6 );
            Assert.That( test.Y == 8 );
            Assert.That( test.Z == 10 );
        }

        [Test]
        public void XyzMultiplyTest()
        {
            XYZ xyz = new XYZ( 1, 2, 3 );
            XYZ test = xyz.Multiply( 10 );
            Assert.That( test.X == 10 );
            Assert.That( test.Y == 20 );
            Assert.That( test.Z == 30 );
        }
    }
}