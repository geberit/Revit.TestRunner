using System.Collections.Generic;
using System.Xml;

namespace Revit.TestRunner.Shared.NUnit
{
    public class NUnitTestSuite : NUnitResult
    {
        public NUnitTestSuite( XmlNode testSuiteNode ) : base( testSuiteNode )
        {
        }

        public NUnitTestSuite[] TestSuites
        {
            get
            {
                XmlNodeList list = Node.SelectNodes( "test-suite" );
                var suites = new List<NUnitTestSuite>();

                foreach( XmlNode node in list ) {
                    suites.Add( new NUnitTestSuite( node ) );
                }

                return suites.ToArray();
            }
        }

        public NUnitTestCase[] TestCases
        {
            get
            {
                XmlNodeList list = Node.SelectNodes( "test-case" );
                var suites = new List<NUnitTestCase>();

                foreach( XmlNode node in list ) {
                    suites.Add( new NUnitTestCase( node ) );
                }

                return suites.ToArray();
            }
        }

        public override string ToString()
        {
            return $"[{Type.ToString().PadRight( 20 )}] {FullName}";
        }
    }
}