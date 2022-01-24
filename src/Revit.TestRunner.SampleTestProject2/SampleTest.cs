using System;
using NUnit.Framework;

namespace Revit.TestRunner.SampleTestProject
{
    public class SampleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Console.WriteLine( $"Run 'OneTimeSetup' in {GetType().Name}" );
        }

        [SetUp]
        public void RunBeforeTest()
        {
            Console.WriteLine( $"Run 'SetUp' in {GetType().Name}" );
        }

        [TearDown]
        public void RunAfterTest()
        {
            Console.WriteLine( $"Run 'TearDown' in {GetType().Name}" );
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Console.WriteLine( $"Run 'OneTimeTearDown' in {GetType().Name}" );
        }

        [Test]
        public void PassTest()
        {
            Assert.True( true );
        }

        [Test]
        public void FailTest()
        {
            Assert.True( false, "This Test should fail!" );
        }
    }
}
