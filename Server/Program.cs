using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private static TcpListener listener;
    private static Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();

    static void Main(string[] args)
    {
        int port = 5000;

        // Start the TCP Listener on the chosen IP and port
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server started on port {port}");

        Directory.CreateDirectory("Files"); // Ensure the "Files" directory exists

        while (true)
        {
            var client = listener.AcceptTcpClient();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];

        // Read the client's name (first message)
        int nameBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
        string clientName = Encoding.UTF8.GetString(buffer, 0, nameBytes).Trim();
        clients[client] = clientName;

        Console.WriteLine($"{clientName} connected.");

        while (true)
        {
            try
            {
                // Read message from the client
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (message == "file_upload")
                {
                    Console.WriteLine($"{clientName} initiated file upload.");
                    string fileName = $"Files/{DateTime.Now:yyyyMMddHHmmss}.txt";
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        Console.WriteLine("Receiving file...");
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            if (bytesRead < buffer.Length) break; // End of file
                        }
                    }
                    Console.WriteLine($"File received and saved as {fileName}");
                    continue;
                }

                Console.WriteLine($"Received from {clientName}: {message}");

                // Broadcast to all clients
                foreach (var c in clients.Keys)
                {
                    if (c != client) // Don't echo back to the sender
                    {
                        var writer = c.GetStream();
                        await writer.WriteAsync(buffer, 0, bytesRead);
                    }
                }
            }
            catch
            {
                Console.WriteLine($"{clientName} disconnected.");
                clients.Remove(client);
                break;
            }
        }
    }
}
