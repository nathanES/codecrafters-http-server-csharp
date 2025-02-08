using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting the server");
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Console.WriteLine("Server started");
var socket = server.AcceptSocket();
socket.Send(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
if (socket.Connected)
{
    Console.WriteLine("ShutingDown the connection");
    socket.Shutdown(SocketShutdown.Both);
}

Console.WriteLine("Closing the connection");
socket.Close();
Console.WriteLine("Connection closed");
Console.WriteLine("Stopping the server");
server.Stop();
Console.WriteLine("Server stopped");
