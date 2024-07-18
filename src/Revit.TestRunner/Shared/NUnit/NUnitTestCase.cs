using System.Xml;

namespace Revit.TestRunner.Shared.NUnit
{
    public class NUnitTestCase : NUnitTestSuite
    {
        private readonly XmlNode mTestCase;

        public NUnitTestCase( XmlNode testCase ) : base( testCase )
        {
            mTestCase = testCase;
        }

        public string StackTrace
        {
            get
            {
                string result = string.Empty;
                XmlNodeList list = mTestCase.SelectNodes( "failure/stack-trace" );

                foreach( XmlNode child in list ) {
                    result = child.InnerText;
                }

                return result;
            }
        }

        public string Output
        {
            get
            {
                string result = string.Empty;
                XmlNodeList list = mTestCase.SelectNodes( "output" );

                foreach( XmlNode child in list ) {
                    result = child.InnerText;
                }

                return result;
            }
        }
    }
}