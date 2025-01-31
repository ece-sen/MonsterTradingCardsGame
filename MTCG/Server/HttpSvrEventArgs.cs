using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTCG.Server
{
    /// <summary>This class defines event arguments for the <see cref="HttpSvrEventHandler"/> event handler.</summary>
    public class HttpSvrEventArgs : EventArgs
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // protected members                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>TCP client.</summary>
        protected TcpClient _Client;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructors                                                                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="client">TCP client.</param>
        /// <param name="plainMessage">Plain HTTP message.</param>
        public HttpSvrEventArgs(TcpClient client, string plainMessage)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));

            PlainMessage = plainMessage ?? throw new ArgumentNullException(nameof(plainMessage));
            Payload = string.Empty;

            string[] lines = plainMessage.Replace("\r\n", "\n").Split('\n');
            bool inHeaders = true;
            List<HttpHeader> headers = new();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    string[] inc = lines[0].Split(' ');
                    if (inc.Length >= 2)
                    {
                        Method = inc[0];
                        Path = inc[1];
                    }
                    else
                    {
                        throw new ArgumentException("Invalid HTTP request line.");
                    }
                    continue;
                }

                if (inHeaders)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        inHeaders = false;
                    }
                    else
                    {
                        headers.Add(new HttpHeader(lines[i]));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Payload)) { Payload += "\r\n"; }
                    Payload += lines[i];
                }
            }

            Headers = headers.ToArray();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets the plain message.</summary>
        public string PlainMessage { get; protected set; } = string.Empty;

        /// <summary>Gets the HTTP method.</summary>
        public virtual string Method { get; protected set; } = string.Empty;

        /// <summary>Gets the HTTP path.</summary>
        public virtual string Path { get; protected set; } = string.Empty;

        /// <summary>Gets the HTTP headers.</summary>
        public virtual HttpHeader[] Headers { get; protected set; } = Array.Empty<HttpHeader>();

        /// <summary>Gets the payload.</summary>
        public virtual string Payload { get; protected set; } = string.Empty;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Replies to the request.</summary>
        /// <param name="status">HTTP Status code.</param>
        /// <param name="body">Reply body.</param>
        public void Reply(int status, string? body = null)
        {
            try
            {
                string data = $"HTTP/1.1 {status} {GetStatusMessage(status)}\n";

                if (string.IsNullOrEmpty(body))
                {
                    data += "Content-Length: 0\n";
                }
                else
                {
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                    data += $"Content-Length: {bodyBytes.Length}\n";
                }
                data += "Content-Type: text/plain\n\n";

                if (!string.IsNullOrEmpty(body))
                {
                    data += body;
                }

                byte[] buf = Encoding.UTF8.GetBytes(data);
                if (_Client?.Connected == true) // ✅ Check if the client is still connected
                {
                    _Client.GetStream().Write(buf, 0, buf.Length);
                    _Client.GetStream().Flush(); // ✅ Ensure all data is sent
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reply: {ex.Message}");
            }
            finally
            {
                // ✅ Delay before closing to ensure response is fully sent
                System.Threading.Thread.Sleep(200); 

                if (_Client?.Connected == true)
                {
                    _Client.Close();
                }
                _Client.Dispose();
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private methods                                                                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets the status message for a given HTTP status code.</summary>
        /// <param name="status">HTTP status code.</param>
        /// <returns>Status message.</returns>
        private static string GetStatusMessage(int status) =>
            status switch
            {
                200 => "OK",
                201 => "Created",
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                _ => "Unknown Status"
            };
    }
}
