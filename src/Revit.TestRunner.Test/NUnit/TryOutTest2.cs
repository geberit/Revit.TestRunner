using System;
using NUnit.Framework;

namespace Revit.TestRunner.Test.NUnit
{
    [TestFixture]
    public class TryOutTest2
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Console.WriteLine( "OneTimeSetUp" );
            //throw new Exception( "OneTimeSetUp" );
        }

        [SetUp]
        public void SetUp()
        {
            Console.WriteLine( "SetUp" );
            //throw new Exception( "SetUp" );
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine( "TearDown" );
            //throw new Exception( "TearDown" );
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Console.WriteLine( "OneTimeTearDown" );
            //throw new Exception( "OneTimeTearDown" );
        }

        [Test]
        public void Test1()
        {
            Console.WriteLine( "Test1" );
            Assert.True( true, "This test assert true" );
        }

        [Test]
        public void Test2()
        {
            Console.WriteLine( "Test2" );
            Assert.True( false, "This test assert false" );
        }

        [Test]
        public void Test3()
        {
            Console.WriteLine( "Test3" );
            Assert.Pass( "All is OK" );
        }
    }
}