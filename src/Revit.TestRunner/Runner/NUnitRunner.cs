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

        public NUnitRunner( string testAssembly )
        {
            TestAssembly = testAssembly ?? throw new ArgumentNullException( nameof( testAssembly ) );
        }
        #endregion

        #region Properties

        private string TestAssembly { get; }

        #endregion

        internal (string File, string Message) ExploreAssembly( string directoryName )
        {
            string result = string.Empty;
            string message = string.Empty;
            ITestRunner testRunner = null;

            try {
                testRunner = CreateTestRunner();
                XmlNode exploreResult = testRunner.Explore( TestFilter.Empty );

                string file = Path.Combine( directoryName, FileNames.ExploreResultFileName );
                exploreResult.OwnerDocument.Save( file );
                result = file;
            }
            catch( Exception e ) {
                message = e.ToString();
            }
            finally {
                testRunner?.Unload();
            }

            return (result, message);
        }

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
    }
}
