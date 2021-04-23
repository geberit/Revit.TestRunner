using System;
using System.IO;
using System.Xml;
using Revit.TestRunner.Shared.Dto;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.Shared.Model
{
    public static class ModelHelper
    {
        /// <summary>
        /// Load a explore response file to show it in the view. 
        /// </summary>
        public static NodeModel ToNodeTree( string exploreFile )
        {
            NodeModel root = null;

            if( File.Exists( exploreFile ) ) {
                StreamReader xmlStreamReader = new StreamReader( exploreFile );
                XmlDocument xmlDoc = new XmlDocument();

                xmlDoc.Load( xmlStreamReader );
                XmlNode rootNode = (XmlElement)xmlDoc.DocumentElement.FirstChild;

                NUnitTestRun run = new NUnitTestRun( rootNode );
                root = ToNodeTree( run );

                
            }

            return root;
        }

        private static NodeModel ToNodeTree( NUnitTestRun run )
        {
            NodeModel root = new NodeModel( run );

            foreach( NUnitTestSuite testSuite in run.TestSuites ) {
                ToNode( root, testSuite );
            }

            return root;
        }

        private static void ToNode( NodeModel parent, NUnitTestSuite testSuite )
        {
            NodeModel node = new NodeModel( testSuite );
            parent.Add( node );
            node.Parent = parent;

            foreach( var suite in testSuite.TestSuites ) {
                ToNode( node, suite );
            }

            foreach( var test in testSuite.TestCases ) {
                ToNode( node, test );
            }
        }

        public static TestCaseDto ToTestCase( NodeModel node )
        {
            if( node == null ) throw new ArgumentNullException();
            if( node.Type != TestType.Case ) throw new ArgumentException( "Only TestCases allowed!" );

            var result = new TestCaseDto {
                Id = node.Id,
                AssemblyPath = node.Root.FullName,
                TestClass = node.ClassName,
                MethodName = node.MethodName
            };

            return result;
        }
    }
}
