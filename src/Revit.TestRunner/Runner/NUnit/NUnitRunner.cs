using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;
using NUnit;
using NUnit.Engine;

namespace Revit.TestRunner.Runner.NUnit
{
    /// <summary>
    /// This Runner runs the corresponding Test using NUnit.
    /// </summary>
    public class NUnitRunner : IDisposable, ITestEventListener
    {
        #region Members, Constructor

        private ITestEngine mEngine;

        public NUnitRunner( string testAssembly )
        {
            TestAssembly = testAssembly ?? throw new ArgumentNullException( nameof( testAssembly ) );
        }
        #endregion

        #region Properties

        public string TestAssembly { get; }

        public NUnitTestRun ExploreRun { get; private set; }

        #endregion

        private ITestEngine CreateTestEngine()
        {
            // Normal way to create a NUnit TestEngine.
            // Not possible because of use AppDomain.CurrentDomain.BaseDirectory which points to bin of Revit.
            //return TestEngineActivator.CreateInstance();

            // Private way to create NUnit TestEngine, using bin directory of TestRunner.
            const string defaultAssemblyName = "nunit.engine.dll";
            const string defaultTypeName = "NUnit.Engine.TestEngine";
            string executionAssemblyPath = Assembly.GetExecutingAssembly().Location;
            var executionAssembly = new FileInfo( executionAssemblyPath );
            string workingDirectory = Path.Combine( executionAssembly.Directory.FullName, defaultAssemblyName );
            

            var engineAssembly = Assembly.ReflectionOnlyLoadFrom( workingDirectory );
            var engine = (ITestEngine)AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap( engineAssembly.CodeBase, defaultTypeName );

            return engine;
        }

        private ITestRunner CreateTestRunner()
        {
            ITestRunner result = null;

            try {
                mEngine = CreateTestEngine();

                TestPackage testPackage = new TestPackage( TestAssembly );

                //https://github.com/nunit/nunit-console/blob/master/src/NUnitEngine/nunit.engine/EnginePackageSettings.cs
                string processModel = "InProcess";
                string domainUsage = "None";
                testPackage.AddSetting( EnginePackageSettings.ProcessModel, processModel );
                testPackage.AddSetting( EnginePackageSettings.DomainUsage, domainUsage );
                result = mEngine.GetRunner( testPackage );
            }
            catch( Exception e ) {
                MessageBox.Show( e.ToString(), "NUnit Engine", MessageBoxButton.OK, MessageBoxImage.Error );
            }

            return result;
        }

        internal void ExploreAssembly()
        {
            ITestRunner testRunner = CreateTestRunner();

            try {
                XmlNode exploreResult = testRunner.Explore( TestFilter.Empty );
                ExploreRun = new NUnitTestRun( exploreResult );

                testRunner.Unload();
            }
            catch( Exception e ) {
                MessageBox.Show( e.ToString(), "Load Assembly", MessageBoxButton.OK, MessageBoxImage.Error );
            }
        }

        /// <summary>
        /// Run Tests with NUnit
        /// https://github.com/nunit/docs/wiki/Test-Filters
        /// </summary>
        internal void Run( string test )
        {
            TestFilter filter = !string.IsNullOrEmpty( test )
                ? new TestFilter( $"<filter><test>{test}</test></filter>" )
                : TestFilter.Empty;

            try {
                ITestRunner testRunner = CreateTestRunner();
                XmlNode runResult = testRunner.Run( this, filter );
                ExploreRun = new NUnitTestRun( runResult );

                testRunner.Unload();
            }
            catch( Exception e ) {
                Log.Debug( e );
                MessageBox.Show( $"{e}", "Exception in NUnit" );
            }
        }

        public void OnTestEvent( string report )
        {
            Log.Debug( report );
        }

        public void Dispose()
        {
            mEngine.Dispose();
        }
    }
}
