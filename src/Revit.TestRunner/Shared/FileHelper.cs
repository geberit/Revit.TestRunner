using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Revit.TestRunner.Shared
{
    public static class FileHelper
    {
        public static DirectoryInfo GetDirectory( string path )
        {
            DirectoryInfo result = new DirectoryInfo( path );

            if( !result.Exists ) result.Create();

            return result;
        }

        public static FileInfo MoveWithLock( string aOriginalFileName, string aDestFileName, int aWait = 100, int aMaxWait = 1000 )
        {
            if( string.IsNullOrEmpty( aOriginalFileName ) ) throw new ArgumentException( nameof( aOriginalFileName ) );
            if( string.IsNullOrEmpty( aDestFileName ) ) throw new ArgumentException( nameof( aDestFileName ) );
            if( aWait < 0 ) throw new ArgumentException( nameof( aWait ) );
            if( aMaxWait < 0 ) throw new ArgumentException( nameof( aMaxWait ) );

            FileInfo result = null;
            bool check = false;

            try {
                check = IoRetry( () => {
                    FileInfo file = new FileInfo( aOriginalFileName );
                    if( file.Exists ) file.MoveTo( aDestFileName );

                }, aWait, aMaxWait );
            }
            catch( FileNotFoundException ) {
            }

            if( check ) result = new FileInfo( aDestFileName );

            return result;
        }

        public static bool DeleteWithLock( string aPath, int aWait = 100, int aMaxWait = 1000 )
        {
            if( aPath == null ) throw new ArgumentNullException( nameof( aPath ) );
            if( aWait < 0 ) throw new ArgumentException( nameof( aWait ) );
            if( aMaxWait < 0 ) throw new ArgumentException( nameof( aMaxWait ) );

            bool result = false;

            try {
                result = IoRetry( () => {
                    FileInfo file = new FileInfo( aPath );
                    if( file.Exists ) file.Delete();

                    DirectoryInfo directory = new DirectoryInfo( aPath );
                    if( directory.Exists ) directory.Delete( true );
                }, aWait, aMaxWait );
            }
            catch( FileNotFoundException ) {
            }
            return result;
        }

        public static string ReadStringWithLock( string aPath, int aWait = 100, int aMaxWait = 1000 )
        {
            if( aPath == null ) throw new ArgumentNullException( nameof( aPath ) );
            if( aWait < 0 ) throw new ArgumentException( nameof( aWait ) );
            if( aMaxWait < 0 ) throw new ArgumentException( nameof( aMaxWait ) );

            string result = null;

            try {
                IoRetry( () => {
                    using( FileStream fs = new FileStream( aPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                    using( StreamReader reader = new StreamReader( fs ) ) {
                        result = reader.ReadToEnd();
                    }
                }, aWait, aMaxWait );
            }
            catch( FileNotFoundException ) {
            }
            return result;
        }

        public static bool WriteStringWithLock( string aPath, string aContent, int aWait = 100, int aMaxWait = 1000 )
        {
            return WriteStringWithLock( aPath, aContent, false, aWait, aMaxWait );
        }

        private static bool WriteStringWithLock( string aPath, string aContent, bool aAppend, int aWait, int aMaxWait )
        {
            if( aContent == null ) throw new ArgumentNullException( nameof( aContent ) );
            if( aPath == null ) throw new ArgumentNullException( nameof( aPath ) );
            if( aWait < 0 ) throw new ArgumentException( nameof( aWait ) );
            if( aMaxWait < 0 ) throw new ArgumentException( nameof( aMaxWait ) );

            bool result = false;

            SetReadWrite( aPath );
            try {
                result = IoRetry( () => {
                    using(
                        FileStream fs = new FileStream( aPath, aAppend ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None )
                    )
                    using( StreamWriter writer = new StreamWriter( fs ) ) {
                        writer.Write( aContent );
                    }
                }, aWait, aMaxWait );
            }
            catch( FileNotFoundException ) {
            }
            return result;
        }

        public static void SetReadWrite( string aPath )
        {
            if( string.IsNullOrEmpty( aPath ) ) throw new ArgumentNullException();

            // ReSharper disable EmptyGeneralCatchClause
            if( File.Exists( aPath ) ) {
                try {
                    File.SetAttributes( aPath, FileAttributes.Normal );
                }
                catch {
                }
            }
            if( Directory.Exists( aPath ) ) {
                try {
                    File.SetAttributes( aPath, FileAttributes.Normal );
                }
                catch {
                }
                try {
                    Directory.GetFiles( aPath ).ToList().ForEach( SetReadWrite );
                }
                catch {
                }
                try {
                    Directory.GetDirectories( aPath ).ToList().ForEach( SetReadWrite );
                }
                catch {
                }
            }

            // ReSharper restore EmptyGeneralCatchClause
        }

        private static bool IoRetry( Action aAction, int aWaitMilliseconds = 100, int aMaxWaitMilliseconds = 1000 )
        {
            if( aAction == null ) throw new ArgumentNullException( nameof( aAction ) );
            if( aWaitMilliseconds < 0 ) throw new ArgumentException( nameof( aWaitMilliseconds ) );
            if( aMaxWaitMilliseconds < 0 ) throw new ArgumentException( nameof( aWaitMilliseconds ) );

            DateTime maxTime = DateTime.Now.AddMilliseconds( aMaxWaitMilliseconds );
            bool result = false;

            do {
                try {
                    aAction();
                    result = true;
                }
                catch( IOException ) {
                    Thread.Sleep( aWaitMilliseconds );
                }
                catch( UnauthorizedAccessException ) {
                    Thread.Sleep( aWaitMilliseconds );
                }
            } while( result == false && DateTime.Now <= maxTime );
            return result;
        }
    }
}
