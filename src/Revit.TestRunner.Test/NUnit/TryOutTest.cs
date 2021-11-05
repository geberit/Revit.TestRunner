using System;
using NUnit.Framework;

namespace Revit.TestRunner.Test.NUnit
{
    [TestFixture]
    public class TryOutTest
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Console.WriteLine( "OneTimeSetUp" );
            throw new Exception( "OneTimeSetUp" );
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
        }

        [Test]
        public void Test2()
        {
            Console.WriteLine( "Test2" );
        }

        [Test]
        public void Test3()
        {
            Console.WriteLine( "Test3" );
        }

    }
}
