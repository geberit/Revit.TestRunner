using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication.Client;
using Revit.TestRunner.Shared.Communication.Server;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Test.Communication
{
    [TestFixture]
    public class CommunicationTest
    {
        private readonly string mTestPath = Path.Combine( Path.GetTempPath(), "TestRunnerCommunicationTest" );

        public CommunicationTest()
        {
            Directory.CreateDirectory( mTestPath );
        }

        [Test]
        public void ServiceTest()
        {
            var basePath = Path.Combine( mTestPath, "ServiceTest" );

            var server = new FileServer( basePath );
            server.RegisterRoute<HomeRequest, HomeResponse>( "", _ => new HomeResponse { AssertString = "HomeString" } );
            server.RegisterRoute<TestRequest, TestResponse>( "test", _ => new TestResponse { AssertString = "TestString" } );

            // Write request to homePath
            JsonHelper.ToFile( Path.Combine( basePath, "001.request" ), new HomeResponse() );
            JsonHelper.ToFile( Path.Combine( basePath, "test", "002.request" ), new TestResponse() );

            server.ProceedNextNotConcurrent();
            server.ProceedNextNotConcurrent();

            Assert.IsTrue( File.Exists( Path.Combine( basePath, "001.response" ) ) );
            Assert.IsTrue( File.Exists( Path.Combine( basePath, "test", "002.response" ) ) );

            var homeResponse = JsonHelper.FromFile<HomeResponse>( Path.Combine( basePath, "001.response" ) );
            var testResponse = JsonHelper.FromFile<HomeResponse>( Path.Combine( basePath, "test", "002.response" ) );
            Assert.AreEqual( homeResponse.AssertString, "HomeString" );
            Assert.AreEqual( testResponse.AssertString, "TestString" );
        }


        [Test]
        public void Communication()
        {
            var basePath = Path.Combine( mTestPath, "Communication" );
            bool gotResponse = false;

            Task serverTask = new Task( () => {
                Console.WriteLine( "Start Server Task" );

                var server = new FileServer( basePath );
                server.RegisterRoute<TestRequest, TestResponse>( "test", request => new TestResponse { AssertString = request.RequestString + request.RequestString } );

                for( int i = 0; i < 10; i++ ) {
                    server.ProceedNextNotConcurrent();
                    Thread.Sleep( 1000 );
                }

                Console.WriteLine( "End Server Task" );
            } );

            Task clientTask = new Task( () => {
                Thread.Sleep( 3000 );
                Console.WriteLine( "Start Client Task" );

                var client = new FileClient( basePath );
                var response = Task.Run( () => client.GetJson<TestRequest, TestResponse>( "test", new TestRequest { RequestString = "TEST" }, CancellationToken.None ) ).Result;
                gotResponse = true;

                Assert.AreEqual( response.AssertString, "TESTTEST" );

                Console.WriteLine( "End Client Task" );
            } );

            serverTask.Start();
            clientTask.Start();

            Task.WaitAny( serverTask, clientTask );

            Assert.IsTrue( gotResponse );
        }

        [Test]
        public void NotFoundTest()
        {
            var client = new FileClient( @"C:\someNonExistingDirectory" );
            Assert.Throws<AggregateException>( () => {
                var response = client.GetJson<TestRequest, TestResponse>( "test", new TestRequest { RequestString = "TEST" }, CancellationToken.None ).Result;
            } );
        }

        [Test]
        public void TimeoutTest()
        {
            var basePath = Path.Combine( mTestPath, "timeout" );
            FileHelper.DeleteWithLock( basePath );
            Directory.CreateDirectory( basePath );

            var client = new FileClient( basePath );
            var response = client.GetJson<TestRequest, TestResponse>( "", new TestRequest { RequestString = "TIMEOUT" }, CancellationToken.None ).Result;

            Assert.IsNull( response );

        }
    }

    public class HomeResponse : BaseResponseDto
    {
        public string AssertString { get; set; }
    }

    public class HomeRequest : BaseRequestDto { }

    public class TestResponse : BaseResponseDto
    {
        public string AssertString { get; set; }
    }

    public class TestRequest : BaseRequestDto
    {
        public string RequestString { get; set; }
    }
}
