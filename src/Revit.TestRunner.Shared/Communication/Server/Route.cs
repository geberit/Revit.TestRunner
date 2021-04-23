using System;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Shared.Communication.Server
{
    /// <summary>
    /// Represents a registered Route.
    /// </summary>
    public abstract class Route
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected Route( string routePath, bool concurrent )
        {
            RoutePath = routePath;
            Concurrent = concurrent;
        }

        /// <summary>
        /// Rout react always, even a long running task is running.
        /// Only for non Revit tasks.
        /// </summary>
        public bool Concurrent { get; }

        /// <summary>
        /// Relative route path.
        /// </summary>
        public string RoutePath { get; }

        /// <summary>
        /// Relative route path for logging. Path contains all requests and responses.
        /// </summary>
        public string LogPath => "__logs\\" + RoutePath;

        /// <summary>
        /// Type of request for this route.
        /// </summary>
        public abstract Type RequestType { get; }

        /// <summary>
        /// Creates a <see cref="Request"/> from the route.
        /// </summary>
        public abstract Request CreateRequest( string file, object request, Route route );
    }

    /// <summary>
    /// Represents a registered Route.
    /// </summary>
    public class Route<TRequest, TResponse> : Route where TResponse : BaseResponseDto
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Route( string routePath, Func<TRequest, TResponse> func, bool concurrent ) : base( routePath, concurrent )
        {
            Func = func;
        }

        /// <summary>
        /// Route function.
        /// </summary>
        public Func<TRequest, TResponse> Func { get; }

        /// <summary>
        /// Type of request for this route.
        /// </summary>
        public override Type RequestType => typeof( TRequest );

        /// <summary>
        /// Creates a <see cref="Request"/> from the route.
        /// </summary>
        public override Request CreateRequest( string file, object request, Route route )
        {
            return new Request<TRequest, TResponse>( System.IO.Path.GetFileNameWithoutExtension( file ), (TRequest)request, route );
        }
    }
}
