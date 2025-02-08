using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Server is starting...");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
try
{
    var socket = server.AcceptSocket();
    try
    {
        await socket.SendAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
    finally
    {
        if (socket.Connected)
        {
            Console.WriteLine("ShutingDown the connection");
            socket.Shutdown(SocketShutdown.Both);
        }

        Console.WriteLine("Closing the connection");
        socket.Close();
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}
finally
{
    server.Stop();
}



