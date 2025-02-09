using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using codecrafters_http_server;

class Program
{
    private static readonly Dictionary<string, Func<HttpRequest, HttpResponse>> RequestHandlers =
        new ()
        {
            {"/", HandleRoot},
        };
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
            var httpRequestLength = await socket.ReceiveAsync(buffer);
            Console.WriteLine($"Received {httpRequestLength} bytes");
            if (httpRequestLength == 0)
                return;
            
            var request = HttpRequest.Parse(buffer, httpRequestLength);
            if (request == null)
            {
                Console.WriteLine("Received null request");
                return;
            }
            Console.WriteLine($"Received request :{JsonSerializer.Serialize(request, new JsonSerializerOptions{WriteIndented = true})}");

            
            await HandleRequest(socket, request); 
            
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
    static async Task HandleRequest(Socket socket, HttpRequest request )
    {
        HttpResponse httpResponse;
        if (RequestHandlers.TryGetValue(request.RequestTarget, out var handler))
        {
            Console.WriteLine("RequestTarget : " + request.RequestTarget);
            httpResponse = handler(request);
        }
        else if (request.RequestTarget.StartsWith("/echo/"))
        {
            Console.WriteLine("RequestTarget : /echo/");
            httpResponse = HandleEcho(request);
        }
        else
        {
            Console.WriteLine("RequestTarget not exists");
            httpResponse = HandleNotFound(request); 
        }
        
        await SendResponse(socket, httpResponse);
        return;
    }

    private static async Task SendResponse(Socket socket, HttpResponse httpResponse)
    { 
        string httpResponseFormatted = httpResponse.Format();
        Console.WriteLine($"Response: {httpResponseFormatted}");
        await socket.SendAsync(Encoding.ASCII.GetBytes(httpResponseFormatted));
    }
    private static HttpResponse HandleEcho(HttpRequest request)
    {
        string value = request.RequestTarget["/echo/".Length..];
        return new HttpResponse.HttpResponseBuilder()
            .SetHttpVersion(request.HttpVersion)
            .SetStatusCode(HttpStatusCode.OK)
            .SetBody(value)
            .Build();
    }

    private static HttpResponse HandleRoot(HttpRequest request)
    {
       return new HttpResponse.HttpResponseBuilder()
            .SetHttpVersion(request.HttpVersion)
            .SetStatusCode(HttpStatusCode.OK)
            .SetBody("Welcome to the HTTP Server!") 
            .Build();
    }

    private static HttpResponse HandleNotFound(HttpRequest request)
    {
        return new HttpResponse.HttpResponseBuilder()
            .SetHttpVersion(request.HttpVersion)
            .SetStatusCode(HttpStatusCode.NotFound)
            .Build(); 
    }


}