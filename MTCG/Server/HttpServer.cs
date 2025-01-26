using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MTCG.Server
{
    /// <summary>This class implements an HTTP server.</summary>
    public sealed class HttpServer
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private members                                                                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>TCP listener instance.</summary>
        private TcpListener? _Listener;

        /// <summary>CancellationTokenSource for stopping the server.</summary>
        private readonly CancellationTokenSource _Cts = new();

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public events                                                                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Is raised when incoming data is available.</summary>
        public event HttpSvrEventHandler? Incoming;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets if the server is available.</summary>
        public bool Active
        {
            get; private set;
        } = false;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Runs the server.</summary>
        public void Run()
        {
            if (Active) return;

            Active = true;
            _Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 12000);
            _Listener.Start();

            Console.WriteLine("Server is running...");

            Task.Run(() => AcceptClientsAsync(_Cts.Token));
        }

        /// <summary>Handles accepting clients asynchronously.</summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await _Listener!.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken));
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Server is stopping...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
            finally
            {
                Active = false;
                _Listener?.Stop();
                Console.WriteLine("Server stopped.");
            }
        }

        /// <summary>Handles an individual client asynchronously.</summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                string data = string.Empty;
                byte[] buf = new byte[256];
                var stream = client.GetStream();

                while (stream.DataAvailable || string.IsNullOrWhiteSpace(data))
                {
                    int n = await stream.ReadAsync(buf.AsMemory(0, buf.Length), cancellationToken);
                    data += Encoding.ASCII.GetString(buf, 0, n);
                }

                Incoming?.Invoke(this, new HttpSvrEventArgs(client, data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        /// <summary>Stops the server.</summary>
        public void Stop()
        {
            if (!Active) return;

            Active = false;
            _Cts.Cancel();

            try
            {
                _Listener?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping the server: {ex.Message}");
            }

            Console.WriteLine("Server has been stopped.");
        }
    }
}
