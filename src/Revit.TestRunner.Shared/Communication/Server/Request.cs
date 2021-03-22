using System;
using System.IO;
using Revit.TestRunner.Shared.Communication.Dto;

namespace Revit.TestRunner.Shared.Communication.Server
{
    /// <summary>
    /// Represets a specific request.
    /// </summary>
    public abstract class Request
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected Request( string id, object request, Route route )
        {
            Id = id;
            Route = route;
            RequestObject = request;
        }

        /// <summary>
        /// Associated <see cref="Server.Route"/>
        /// </summary>
        protected Route Route { get; }

        /// <summary>
        /// Request object.
        /// </summary>
        protected object RequestObject { get; }

        /// <summary>
        /// Request Id. 
        /// </summary>
        protected string Id { get; }

        /// <summary>
        /// Execute the Route function.
        /// </summary>
        public abstract void Execute( string basePath );
    }

    /// <summary>
    /// Represets a specific request.
    /// </summary>
    public class Request<TRequest, TResponse> : Request where TResponse : BaseResponseDto
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Request( string id, TRequest request, Route route ) : base( id, request, route )
        {
        }

        /// <summary>
        /// Request object.
        /// </summary>
        protected new TRequest RequestObject => (TRequest)base.RequestObject;

        /// <summary>
        /// Execute the Route function.
        /// </summary>
        public override void Execute( string basePath )
        {
            var route = (Route<TRequest, TResponse>)Route;

            var response = route.Func( RequestObject );
            var responseFilePath = Path.Combine( basePath, Route.RoutePath, $"{Id}.response" );

            response.Timestamp = DateTime.Now;
            response.RequestId = Id;
            response.ThisPath = responseFilePath;

            JsonHelper.ToFile( responseFilePath, response );
        }
    }
}
