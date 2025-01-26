using System;
using MTCG.Server;

namespace MTCG
{
    /// <summary>This class contains the main entry point of the application.</summary>
    internal class Program
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public constants                                                                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Determines if debug token ("UserName-debug") will be accepted.</summary>
        public const bool ALLOW_DEBUG_TOKEN = true;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // entry point                                                                                                      //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Application entry point.</summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");

            HttpServer svr = new();
            svr.Incoming += Svr_Incoming;

            try
            {
                svr.Run();
                Console.WriteLine("Server is running. Press Ctrl+C to stop.");

                // Block the main thread and wait for shutdown signal
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // Prevent the application from terminating immediately
                    svr.Stop();
                };

                // Keep the application running until the server stops
                while (svr.Active)
                {
                    Thread.Sleep(100); // Small delay to prevent CPU overutilization
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while running the server: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Server has been stopped.");
            }
        }

        /// <summary>Handles incoming HTTP requests.</summary>
        private static void Svr_Incoming(object sender, HttpSvrEventArgs e)
        {
            try
            {
                // Use the handler to process the request
                Handler.HandleEvent(e);

                // Optionally, add detailed logging for debugging
                Console.WriteLine($"Request received:");
                Console.WriteLine($"Method: {e.Method}");
                Console.WriteLine($"Path: {e.Path}");
                foreach (HttpHeader header in e.Headers)
                {
                    Console.WriteLine($"{header.Name}: {header.Value}");
                }
                Console.WriteLine($"Payload: {e.Payload}");
            }
            catch (Exception ex)
            {
                // Log errors while processing the request
                Console.WriteLine($"Error handling request: {ex.Message}");
                e.Reply(HttpStatusCode.INTERNAL_SERVER_ERROR, "An internal server error occurred.");
            }
        }
    }
}
