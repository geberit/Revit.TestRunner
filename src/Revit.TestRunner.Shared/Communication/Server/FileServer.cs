using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Shared.Communication.Server
{
    /// <summary>
    /// Server for File based communication
    /// </summary>
    public class FileServer
    {
        private readonly List<Route> mRoutes;

        /// <summary>
        /// Constructor. Create Folder if not exist.
        /// </summary>
        public FileServer( string basePath )
        {
            BasePath = basePath;
            mRoutes = new List<Route>();
        }

        /// <summary>
        /// Get the base path.
        /// </summary>
        private string BasePath { get; }

        /// <summary>
        /// Register a route. Route is relative to <see cref="BasePath"/>.
        /// </summary>
        public void RegisterRoute<TRequest, TResponse>( string route, Func<TRequest, TResponse> func, bool concurrent = false )
            where TRequest : BaseRequestDto
            where TResponse : BaseResponseDto
        {
            if( mRoutes.Select( r => r.RoutePath ).Contains( route ) ) {
                throw new Exception( $"Route already registered! {route}" );
            }

            var routeObject = new Route<TRequest, TResponse>( route, func, concurrent );

            mRoutes.Add( routeObject );

            var routeDirectory = new DirectoryInfo( Path.Combine( BasePath, route ) );
            if( !routeDirectory.Exists ) routeDirectory.Create();
            routeDirectory.GetFiles().ToList().ForEach( f => FileHelper.DeleteWithLock( f.FullName ) );
        }

        /// <summary>
        /// Start endless loop of all concurrent routes.
        /// </summary>
        public void StartConcurrentRoutes()
        {
            Task.Run( () => {
                while( true ) {
                    foreach( var route in mRoutes.Where( r => r.Concurrent ) ) {
                        var request = GetRouteRequest( route );

                        if( request != null ) {
                            request.Execute( BasePath );
                            break;
                        }
                    }

                    Thread.Sleep( 200 );
                }
            } );
        }

        /// <summary>
        /// Process next Request. Order from Route registration.
        /// </summary>
        public void ProceedNextNotConcurrent()
        {
            foreach( var route in mRoutes.Where( r => !r.Concurrent ) ) {
                var request = GetRouteRequest( route );

                if( request != null ) {
                    request.Execute( BasePath );
                    break;
                }
            }
        }

        /// <summary>
        /// Get request from <paramref name="route"/>. The request File will be deletet.
        /// Returns null if no pending request in <paramref name="route"/>.
        /// </summary>
        private Request GetRouteRequest( Route route )
        {
            Request result = null;

            var files = Directory.GetFiles( Path.Combine( BasePath, route.RoutePath ), "*.request" );
            var file = files.FirstOrDefault();

            if( file != null ) {
                try {
                    var request = JsonHelper.FromFile( file, route.RequestType );
                    if( request == null ) throw new NullReferenceException( nameof( request ) );

                    result = route.CreateRequest( Path.GetFileNameWithoutExtension( file ), request, route );
                }
                finally {
                    FileHelper.DeleteWithLock( file );
                }
            }

            return result;
        }
    }
}
