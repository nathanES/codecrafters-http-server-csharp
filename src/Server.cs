using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\nShutdown signal received...");
            e.Cancel = true; // Prevents immediate exit
            cts.Cancel(); // Triggers server shutdown
        };
        Console.WriteLine("Starting the server...");
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started on port 4221");

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Waiting for client connection...");
                    var socket = await server.AcceptSocketAsync(cts.Token);
                    Console.WriteLine("Client connected");
                    _ = Task.Run(() => HandleClient(socket), cts.Token);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Server shutting down...");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            server.Stop();
            Console.WriteLine("Server stopped.");
        }
    }

    static async Task HandleClient(Socket socket)
    {
        try
        {
            var buffer = new byte[1024];
            int requestLength = await socket.ReceiveAsync(buffer);
            Console.WriteLine($"Received {requestLength} bytes");
            if (requestLength == 0)
                return;
            var request = Request.Create(buffer, requestLength);
            Console.WriteLine($"Received request :{JsonSerializer.Serialize(request, new JsonSerializerOptions{WriteIndented = true})}");


            switch (request.RequestTarget)
            {
                case "/":
                    Console.WriteLine("RequestTarget exists");
                    await socket.SendAsync(Encoding.ASCII.GetBytes($"{request.HttpVersion} 200 OK\r\n\r\n"));
                    break;
                default:
                    Console.WriteLine("RequestTarget not exists");
                    await socket.SendAsync(Encoding.ASCII.GetBytes($"{request.HttpVersion} 404 Not Found\r\n\r\n"));
                    break;
            }
            
            Console.WriteLine("ShuttingDown the Socket connection...");
            socket.Shutdown(SocketShutdown.Both);
            Console.WriteLine("Socket connection is shut down");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Client error: {e.Message}");
        }
        finally
        {
            Console.WriteLine("Closing the Socket connection...");
            socket.Close();
            Console.WriteLine("Socket Connection closed");
        }
    }

    public class Request
    {
        public string HttpMethod { get; private set; }
        public string RequestTarget { get; private set; }
        public string HttpVersion { get; private set; }

        public class RequestBuilder
        {
            private Request _request;

            public RequestBuilder()
            {
                _request = new Request();
            }

            public RequestBuilder SetHttpMethod(string httpMethod)
            {
                _request.HttpMethod = httpMethod;
                return this;
            }
            public RequestBuilder SetRequestTarget(string requestTarget)
            {
                _request.RequestTarget = requestTarget;
                return this;
            } 
            public RequestBuilder SetHttpVersion(string httpVersion)
            {
                _request.HttpVersion = httpVersion;
                return this;
            } 
            public Request Build()
            {
                return _request;
            }
        }
        public static Request Create(byte[] request, int requestLength)
        {
            return Create(Encoding.ASCII.GetString(request, 0, requestLength));
        }

        public static Request Create(string request)
        {
              var requestParts = request.Split("\r\n");
              var requestLine = requestParts[0].Split(" ");
              return new RequestBuilder()
                  .SetHttpMethod(requestLine[0])
                  .SetRequestTarget(requestLine[1])
                  .SetHttpVersion(requestLine[2])
                  .Build();
        }
    }
}