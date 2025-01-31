using System;

namespace MTCG.Server
{
    /// <summary>This enumeration defines HTTP status codes that are used by
    ///          the <see cref="HttpServer"/> implementation.</summary>
    public static class HttpStatusCode
    {
        /// <summary>Status code OK.</summary>
        public const int OK = 200;

        /// <summary>Status code BAD REQUEST.</summary>
        public const int BAD_REQUEST = 400;

        /// <summary>Status code UNAUTHORIZED.</summary>
        public const int UNAUTHORIZED = 401;

        /// <summary>Status code NOT FOUND.</summary>
        public const int NOT_FOUND = 404;
        
        //<summary>Status code 201.</summary>
        public const int CREATED = 201;
        
        //<summary>Status code 409.</summary>
        public const int CONFLICT = 409;
        
        //<summary>Status code 201.</summary>
        public const int INTERNAL_SERVER_ERROR = 500;

    }
}