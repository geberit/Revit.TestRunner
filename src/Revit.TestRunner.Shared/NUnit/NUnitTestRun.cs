using System.Collections.Generic;
using System.Xml;

namespace Revit.TestRunner.Shared.NUnit
{
    /// <summary>
    /// Representiert einen Test Run
    /// </summary>
    public class NUnitTestRun : NUnitResult
    {
        public NUnitTestRun( XmlNode aNode ) : base( aNode )
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
    }
}