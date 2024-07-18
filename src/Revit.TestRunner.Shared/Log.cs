using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace Revit.TestRunner
{
    public static class Log
    {
        #region Constants, Members
        private const string RepositoryName = "Revit.TestRunner";
        private const string LogFileName = "Test.Runner.log";

        private static ILog sLogger = null;
        #endregion

        #region Properties
        private static ILog Logger => sLogger ?? ( sLogger = SetupLog() );

        public static string LogDirectory
        {
            get
            {
                var appenders = Logger.Logger.Repository.GetAppenders();

                foreach( IAppender appender in appenders ) {
                    if( appender is RollingFileAppender fileAppender ) {
                        return Path.GetDirectoryName( fileAppender.File );
                    }
                }
                return "-";
            }
        }

        public static string LogFilePath => Path.Combine( LogDirectory, LogFileName );
        #endregion

        #region Methods

        public static void Debug( object message )
        {
            Logger.Debug( message );
        }

        public static void Info( object message )
        {
            Logger.Info( message );
        }

        public static void Warn( object message )
        {
            Logger.Warn( message );
        }

        public static void Error( object message )
        {
            Logger.Error( message );
        }

        public static void Error( object message, Exception exception )
        {
            Logger.Error( message, exception );
        }


        private static ILog SetupLog()
        {
            string bin = Assembly.GetExecutingAssembly().CodeBase;
            bin = bin.Replace( @"file:///", string.Empty );
            bin = Path.GetDirectoryName( bin );

            string logFile = Path.Combine( bin, LogFileName );

            LogManager.CreateRepository( RepositoryName );
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository( RepositoryName );

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
            patternLayout.ActivateOptions();

            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = logFile;
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 5;
            roller.MaximumFileSize = "6MB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();
            hierarchy.Root.AddAppender( roller );

            ConsoleAppender console = new ConsoleAppender();
            console.Layout = patternLayout;
            console.ActivateOptions();
            hierarchy.Root.AddAppender( console );

            hierarchy.Root.Level = log4net.Core.Level.Debug;
            hierarchy.Configured = true;

            ILog log = LogManager.GetLogger( RepositoryName, typeof( Log ) );

            if( log == null ) throw new NullReferenceException( "Log not initialized!" );

            return log;
        }
        #endregion
    }
}
