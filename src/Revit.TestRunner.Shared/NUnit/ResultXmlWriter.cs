using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Shared.NUnit
{
    /// <summary>
    /// Write a NUnit result xml file.
    /// https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html#test-suite
    /// </summary>
    public class ResultXmlWriter
    {
        private readonly FileInfo mFile;
        private readonly XmlDocument mDoc;

        /// <summary>
        /// Constructor
        /// </summary>
        public ResultXmlWriter( string filePath )
        {
            if( string.IsNullOrEmpty( filePath ) ) throw new ArgumentNullException();

            mFile = new FileInfo( filePath );
            mDoc = new XmlDocument();
        }

        /// <summary>
        /// Write the result xml file.
        /// </summary>
        public void Write( TestRunStateDto runStateDto )
        {
            CleanFile();

            if( runStateDto.Cases == null ) runStateDto.Cases = Array.Empty<TestCaseDto>();

            var environment = GetEnvironment();
            var testRun = GetTestRun( runStateDto );
            var testSuites = GetCompleteTestSuites( runStateDto );

            testRun.AppendChild( environment );
            foreach( var ts in testSuites ) {
                testRun.AppendChild( ts );
            }

            mDoc.AppendChild( testRun );

            using( TextWriter sw = new StreamWriter( mFile.FullName, false, Encoding.UTF8 ) ) {
                mDoc.Save( sw );
            }
        }

        /// <summary>
        /// Create Directory, delete existing file.
        /// </summary>
        private void CleanFile()
        {
            FileHelper.GetDirectory( mFile.Directory.FullName );

            if( mFile.Exists ) {
                FileHelper.DeleteWithLock( mFile.FullName );
            }
        }

        /// <summary>
        /// Get a new environment element.
        /// </summary>
        private XmlElement GetEnvironment()
        {
            var result = mDoc.CreateElement( "environment" );
            result.SetAttribute( "os-version", Environment.OSVersion.VersionString );
            result.SetAttribute( "clr-version", Environment.Version.ToString() );
            result.SetAttribute( "platform", Environment.OSVersion.Platform.ToString() );
            result.SetAttribute( "cwd", Environment.CurrentDirectory );
            result.SetAttribute( "machine-name", Environment.MachineName );
            result.SetAttribute( "user", Environment.UserName );
            result.SetAttribute( "user-domain", Environment.UserDomainName );
            result.SetAttribute( "culture", CultureInfo.CurrentCulture.ToString() );
            result.SetAttribute( "uiculture", CultureInfo.CurrentUICulture.ToString() );
            result.SetAttribute( "os-architecture", RuntimeInformation.ProcessArchitecture.ToString() );

            return result;
        }

        /// <summary>
        /// Get a new test-run element representing the test run.
        /// </summary>
        private XmlElement GetTestRun( TestRunStateDto runStateDto )
        {
            var result = mDoc.CreateElement( "test-run" );
            result.SetAttribute( "id", runStateDto.Id );
            result.SetAttribute( "testcasecount", runStateDto.Cases.Length.ToString() );
            result.SetAttribute( "result", runStateDto.State.ToString() );
            result.SetAttribute( "total", runStateDto.Cases.Count( c => c.State == TestState.Failed || c.State == TestState.Passed ).ToString() );
            result.SetAttribute( "passed", runStateDto.Cases.Count( c => c.State == TestState.Passed ).ToString() );
            result.SetAttribute( "failed", runStateDto.Cases.Count( c => c.State == TestState.Failed ).ToString() );
            result.SetAttribute( "skipped", runStateDto.Cases.Count( c => c.State == TestState.Ignore ).ToString() );
            result.SetAttribute( "clr-version", Environment.Version.ToString() );
            result.SetAttribute( "start-time", runStateDto.StartTime.ToString( CultureInfo.InvariantCulture ) );
            result.SetAttribute( "end-time", runStateDto.EndTime.ToString( CultureInfo.InvariantCulture ) );
            result.SetAttribute( "duration", (runStateDto.EndTime - runStateDto.StartTime).ToString() );

            return result;
        }

        /// <summary>
        /// Get new test-suite elements with sub elements representing all test cases.
        /// Each root test-suite element representing an assembly.
        /// </summary>
        private IEnumerable<XmlElement> GetCompleteTestSuites( TestRunStateDto runStateDto )
        {
            var result = new List<XmlElement>();
            var assemblyGroups = runStateDto.Cases.GroupBy( tc => tc.AssemblyPath );

            foreach( IGrouping<string, TestCaseDto> assemblyGroup in assemblyGroups ) {
                var assemblyTestSuite = mDoc.CreateElement( "test-suite" );
                assemblyTestSuite.SetAttribute( "type", "Assembly" );
                assemblyTestSuite.SetAttribute( "fullname", new FileInfo( assemblyGroup.Key ).Name );
                assemblyTestSuite.SetAttribute( "fullname", assemblyGroup.Key );
                assemblyTestSuite.SetAttribute( "total", assemblyGroup.Count().ToString() );
                assemblyTestSuite.SetAttribute( "passed", assemblyGroup.Count( c => c.State == TestState.Passed ).ToString() );
                assemblyTestSuite.SetAttribute( "failed", assemblyGroup.Count( c => c.State == TestState.Failed ).ToString() );
                assemblyTestSuite.SetAttribute( "skipped", assemblyGroup.Count( c => c.State == TestState.Ignore ).ToString() );
                assemblyTestSuite.SetAttribute( "time", GetTime( assemblyGroup ).ToString() );
                result.Add( assemblyTestSuite );

                var classGroups = assemblyGroup.GroupBy( tc => tc.TestClass );

                foreach( IGrouping<string, TestCaseDto> classGroup in classGroups ) {
                    var classTestSuite = mDoc.CreateElement( "test-suite" );
                    classTestSuite.SetAttribute( "type", "TestFixture" );
                    classTestSuite.SetAttribute( "name", GetLast( classGroup.Key ) );
                    classTestSuite.SetAttribute( "fullname", classGroup.Key );
                    classTestSuite.SetAttribute( "total", classGroup.Count().ToString() );
                    classTestSuite.SetAttribute( "passed", classGroup.Count( c => c.State == TestState.Passed ).ToString() );
                    classTestSuite.SetAttribute( "failed", classGroup.Count( c => c.State == TestState.Failed ).ToString() );
                    classTestSuite.SetAttribute( "skipped", classGroup.Count( c => c.State == TestState.Ignore ).ToString() );
                    classTestSuite.SetAttribute( "time", GetTime( classGroup ).ToString() );
                    assemblyTestSuite.AppendChild( classTestSuite );

                    foreach( TestCaseDto caseDto in classGroup ) {
                        var testCase = mDoc.CreateElement( "test-case" );
                        testCase.SetAttribute( "id", caseDto.Id );
                        testCase.SetAttribute( "name", caseDto.MethodName );
                        testCase.SetAttribute( "fullname", $"{caseDto.TestClass}.{caseDto.MethodName}" );
                        testCase.SetAttribute( "result", caseDto.State.ToString() );
                        testCase.SetAttribute( "time", (caseDto.EndTime - caseDto.StartTime).ToString() );
                        testCase.SetAttribute( "assets", "0" );

                        if( caseDto.State == TestState.Failed ) {
                            var failure = mDoc.CreateElement( "failure" );
                            testCase.AppendChild( failure );

                            if( !string.IsNullOrEmpty( caseDto.Message ) ) {
                                var message = mDoc.CreateElement( "message" );
                                message.InnerText = caseDto.Message;
                                failure.AppendChild( message );
                            }

                            if( !string.IsNullOrEmpty( caseDto.StackTrace ) ) {
                                var stackTrace = mDoc.CreateElement( "stack-trace" );
                                stackTrace.InnerText = caseDto.StackTrace;
                                failure.AppendChild( stackTrace );
                            }
                        }

                        classTestSuite.AppendChild( testCase );
                    }

                }
            }


            return result;
        }

        /// <summary>
        /// Get the last segment of the string, split by '.'.
        /// </summary>
        private string GetLast( string inputString )
        {
            var split = inputString.Split( '.' );
            return split.Last();
        }

        /// <summary>
        /// Get the total time of all <paramref name="caseDtos"/>.
        /// </summary>
        private TimeSpan GetTime( IEnumerable<TestCaseDto> caseDtos )
        {
            var result = new TimeSpan();
            caseDtos.ToList().ForEach( c => result += (c.EndTime - c.StartTime) );

            return result;
        }
    }
}
