using System;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit;
using NUnit.Engine;
using NUnit.Engine.Services;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Runner
{
    /// <summary>
    /// This Runner runs the corresponding Test using NUnit.
    /// </summary>
    public class NUnitRunner : IDisposable
    {
        #region Members, Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="testAssembly">Assembly to be tested</param>
        /// <param name="outputDirectory">Directory for Runner output</param>
        public NUnitRunner( string testAssembly, string outputDirectory )
        {
            TestAssembly = testAssembly ?? throw new ArgumentNullException( nameof( testAssembly ) );

            if( !Directory.Exists( outputDirectory ) ) throw new DirectoryNotFoundException( nameof( testAssembly ) );
            OutputDirectory = outputDirectory;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Get test assembly to be used.
        /// </summary>
        private string TestAssembly { get; }

        /// <summary>
        /// Get the directory for the output.
        /// </summary>
        private string OutputDirectory { get; }

        /// <summary>
        /// Get the path of the explore result file.
        /// </summary>
        internal string ExploreResultFile => Path.Combine( OutputDirectory, FileNames.ExploreResultFileName );
        #endregion

        #region Methds


        /// <summary>
        /// Explore the assembly using nUnit.
        /// </summary>
        /// <returns>Exception message</returns>
        internal string ExploreAssembly()
        {
            string message = string.Empty;
            ITestRunner testRunner = null;

            try {
                testRunner = CreateTestRunner();
                XmlNode exploreResult = testRunner.Explore( TestFilter.Empty );

                exploreResult.OwnerDocument.Save( ExploreResultFile );
            }
            catch( Exception e ) {
                message = e.ToString();
            }
            finally {
                testRunner?.Unload();
            }

            return message;
        }

        /// <summary>
        /// Create the nUnit test runner.
        /// </summary>
        private ITestRunner CreateTestRunner()
        {
            ITestRunner result = null;
            ITestEngine engine = CreateTestEngine();

            TestPackage testPackage = new TestPackage( TestAssembly );

            //https://github.com/nunit/nunit-console/blob/master/src/NUnitEngine/nunit.engine/EnginePackageSettings.cs
            string processModel = "InProcess";
            string domainUsage = "None";
            testPackage.AddSetting( EnginePackageSettings.ProcessModel, processModel );
            testPackage.AddSetting( EnginePackageSettings.DomainUsage, domainUsage );
            result = engine.GetRunner( testPackage );

            var agency = engine.Services.GetService<TestAgency>();
            agency?.StopService();

            return result;
        }

        /// <summary>
        /// Create the nUnit test engine.
        /// </summary>
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

        public void Dispose()
        {
        } 
        #endregion
    }
}
