using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using codecrafters_http_server;

class Program
{
    private static readonly Dictionary<string, Func<HttpRequest, HttpResponse>> _requestHandlers =
        new ()
        {
            {"/", HandleRoot},
            {"/user-agent", HandleUserAgent},
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
        if (_requestHandlers.TryGetValue(request.RequestTarget, out var handler))
        {
            Console.WriteLine("RequestTarget : " + request.RequestTarget);
            httpResponse = handler(request);
        }
        else if (request.RequestTarget.StartsWith("/echo/"))
        {
            Console.WriteLine("RequestTarget : /echo/");
            httpResponse = HandleEcho(request);
        }
        else if (request.RequestTarget.StartsWith("/files/"))
        {
            Console.WriteLine("RequestTarget : /files/");
            httpResponse = HandleFiles(request);
        }
        else
        {
            Console.WriteLine("RequestTarget not exists");
            httpResponse = HandleNotFound(request, "RequestTarget not exists"); 
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


    private static HttpResponse HandleRoot(HttpRequest request)
    {
       return new HttpResponse.HttpResponseBuilder()
            .SetHttpVersion(request.HttpVersion)
            .SetStatusCode(HttpStatusCode.OK)
            .SetBody("Welcome to the HTTP Server!") 
            .Build();
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

    private static HttpResponse HandleUserAgent(HttpRequest request)
    {
        return new HttpResponse.HttpResponseBuilder()
            .SetHttpVersion(request.HttpVersion)
            .SetStatusCode(HttpStatusCode.OK)
            .SetBody(request.Headers.TryGetValue("User-Agent", out var header) ? header : string.Empty)
            .Build();
    }

    private static HttpResponse HandleFiles(HttpRequest request)
    {
        HashSet<string> keyValueArgumentsHandled = new HashSet<string>(){"--directory"};
        var argv = ParseKeyValueArgs(keyValueArgumentsHandled, Environment.GetCommandLineArgs().Skip(1).ToArray());
        if ((!argv.TryGetValue("--directory", out string? directoryPath) || !string.IsNullOrEmpty(directoryPath)) 
            && !Directory.Exists(directoryPath))
        {
            Console.WriteLine("Directory path is missing or does not exist.");
            return HandleNotFound(request, "Directory path is missing or does not exist.");
        }
        
        string fileNameToCreate = request.RequestTarget["/files/".Length..];
        if (string.IsNullOrEmpty(fileNameToCreate))
        {
            Console.WriteLine("File name is missing.");
            return HandleNotFound(request, "File name is missing.");
        }
        string filePath = Path.Combine(directoryPath, fileNameToCreate);
        if (File.Exists(filePath))
        {
            File.AppendAllText(filePath, request.Body);
            return new HttpResponse.HttpResponseBuilder()
                .SetHttpVersion(request.HttpVersion)
                .SetStatusCode(HttpStatusCode.OK)
                .SetHeader("Content-Type", "application/octet-stream")
                .Build();
        }
        else
        {
            File.WriteAllText(filePath, request.Body);
            return new HttpResponse.HttpResponseBuilder()
                .SetHttpVersion(request.HttpVersion)
                .SetStatusCode(HttpStatusCode.Created)
                .SetHeader("Content-Type", "application/octet-stream")
                .Build();
        }
        
        
    }

    private static Dictionary<string, string> ParseKeyValueArgs(HashSet<string> keyValueArgumentsHandled, string[] arguments)
    {
        if(arguments == null || arguments.Length == 0)
            return new Dictionary<string, string>();

        var argumentsResult = new Dictionary<string, string>();
 
        for (int i = 0; i < arguments.Length; i++)
        {
            if(!keyValueArgumentsHandled.Contains(arguments[i]) || i + 1 >= arguments.Length)
                continue;
            
            if(!argumentsResult.ContainsKey(arguments[i]))
                argumentsResult.Add(arguments[i], arguments[i+1]);
            i++;
        }
        return argumentsResult;
    }

    private static HttpResponse HandleNotFound(HttpRequest request, string body)
    {
        return new HttpResponse.HttpResponseBuilder()
            .SetHttpVersion(request.HttpVersion)
            .SetStatusCode(HttpStatusCode.NotFound)
            .SetBody(body)
            .Build(); 
    }


}