using System;
using System.IO;
using NUnit.Framework;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Dto;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.Test.NUnit
{
    [TestFixture]
    public class ResultXmlFileWriterTest
    {
        [Test]
        public void WriteXmlFile()
        {
            var exampleRunTestStateString = File.ReadAllText( "NUnit\\example.json" );
            var exampleRunTestStateDto = JsonHelper.FromString<TestRunStateDto>( exampleRunTestStateString );

            var outputFile = new FileInfo( @"C:\temp\test.runner\result.xml" );
            FileHelper.DeleteWithLock( outputFile.FullName );
            Assert.IsFalse( File.Exists( outputFile.FullName ) );

            ResultXmlWriter writer = new ResultXmlWriter( outputFile.FullName );
            writer.Write( exampleRunTestStateDto );

            Assert.IsTrue( File.Exists( outputFile.FullName ) );
            Assert.Greater( outputFile.Length, 1000 );
            Assert.Greater( DateTime.Now, outputFile.LastWriteTime );
        }
    }
}
