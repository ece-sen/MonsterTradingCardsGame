﻿using System;
using System.Reflection;
using System.Text.Json.Nodes;



namespace MTCG.Server
{
    /// <summary>This class provides an abstract implementation of the
    /// <see cref="IHandler"/> interface. It also implements static methods
    /// that handles an incoming HTTP request by discovering and calling
    /// available handlers.</summary>
    public abstract class Handler: IHandler
    {
        /// <summary>List of available handlers.</summary>
        private static List<IHandler>? _Handlers = null;
        
        /// <summary>Discovers and returns all available handler implementations.</summary>
        /// <returns>Returns a list of available handlers.</returns>
        private static readonly object _HandlerLock = new();
        private static List<IHandler> _GetHandlers()
        {
            List<IHandler> rval = new();

            foreach(Type i in Assembly.GetExecutingAssembly().GetTypes()
                              .Where(m => m.IsAssignableTo(typeof(IHandler)) && (!m.IsAbstract)))
            {                                                                   // iterate all concrete types that implement IHandler
                IHandler? h = (IHandler?) Activator.CreateInstance(i);          // create an instance
                if(h != null)
                {                                                               // add to result set
                    rval.Add(h);
                }
            }

            return rval;
        }
        
        /// <summary>Handles an incoming HTTP request.</summary>
        /// <param name="e">Event arguments.</param>
        public static void HandleEvent(HttpSvrEventArgs e)
        {
            _Handlers ??= _GetHandlers();                                       // initialize handlers if needed

            foreach(IHandler i in _Handlers)
            {                                                                   // iterate handlers to find one that handles the request
                if(i.Handle(e)) return;
            }
                                                                                // reply 400 if no handler was able to process the request
            e.Reply(HttpStatusCode.BAD_REQUEST, new JsonObject() { ["success"] = false, ["message"] = "Bad request" }.ToJsonString());
        }
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [interface] IHandler                                                                                             //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Tries to handle a HTTP request.</summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>Returns TRUE if the request was handled by this instance,
        ///          otherwise returns FALSE.</returns>
        public abstract bool Handle(HttpSvrEventArgs e);
    }
}