using System.Xml;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.NUnit
{
    public abstract class NUnitResult
    {
        protected NUnitResult( XmlNode aNode )
        {
            Node = aNode ?? throw new System.ArgumentNullException( nameof( aNode ) );
        }

        #region Properties

        protected XmlNode Node { get; }

        public TestState Result => ToState( NodeAttribute( "result" ) );

        public string Name => NodeAttribute( "name" );

        public string FullName => NodeAttribute( "fullname" );

        public string Id => NodeAttribute( "id" );

        public string ClassName => NodeAttribute( "classname" );

        public string MethodName => NodeAttribute( "methodname" );

        public TestType Type => ToType();

        public string Message
        {
            get
            {
                string result = string.Empty;

                if( Result == TestState.Failed ) {
                    XmlNodeList list = Node.SelectNodes( "failure/message" );

                    foreach( XmlNode child in list ) {
                        result = child.InnerText;
                    }
                }
                else if( Result == TestState.Passed ) {
                    XmlNodeList list = Node.SelectNodes( "output" );

                    foreach( XmlNode child in list ) {
                        result = child.InnerText;
                    }
                }

                return result;
            }
        }

        public string FailureStackTrace
        {
            get
            {
                string result = string.Empty;
                XmlNodeList list = Node.SelectNodes( "failure/stack-trace" );

                foreach( XmlNode child in list ) {
                    result = child.InnerText;
                }

                return result;
            }
        }
        #endregion

        #region Methods

        private string NodeAttribute( string attributeName )
        {
            string result = string.Empty;
            var attribute = Node.Attributes[ attributeName ];

            if( attribute != null ) result = attribute.Value;

            return result;
        }

        private TestState ToState( string aState )
        {
            TestState result = TestState.Unknown;

            if( aState == "Passed" ) result = TestState.Passed;
            if( aState == "Failed" ) result = TestState.Failed;

            return result;
        }

        private TestType ToType()
        {
            TestType result = TestType.Unknown;

            string type = NodeAttribute( "type" );

            if( !string.IsNullOrEmpty( type ) ) {
                if( type == "TestSuite" ) result = TestType.Suite;
                if( type == "TestFixture" ) result = TestType.Fixture;
                if( type == "Assembly" ) result = TestType.Assembly;
            }

            if( Node.Name == "test-suite" ) result = TestType.Suite;
            if( Node.Name == "test-run" ) result = TestType.Run;
            if( Node.Name == "test-case" ) result = TestType.Case;

            return result;
        }

        public override string ToString()
        {
            return $"{GetType().Name.PadRight( 20 )} {FullName}";
        }

        #endregion

    }

    

    public enum TestType
    {
        Unknown,
        Run,
        Assembly,
        Suite,
        Fixture,
        Case
    }
}
