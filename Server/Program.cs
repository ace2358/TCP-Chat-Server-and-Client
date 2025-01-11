using System;
using System.Collections.Generic;
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
        Console.WriteLine("Choose server IP:");
        Console.WriteLine("1. Local IP");
        Console.WriteLine("2. Public IP");
        string choice = Console.ReadLine();

        string ipAddress;
        int port = 5000;

        // Choose the type of IP to use
        if (choice == "1")
        {
            ipAddress = GetLocalIPAddress();
            Console.WriteLine($"Server will start on Local IP: {ipAddress}");
        }
        else if (choice == "2")
        {
            ipAddress = GetPublicIPAddress();
            Console.WriteLine($"Server will start on Public IP: {ipAddress}");
        }
        else
        {
            Console.WriteLine("Invalid choice. Using Local IP by default.");
            ipAddress = GetLocalIPAddress();
        }

        // Start the TCP Listener on the chosen IP and port
        TcpListener listener = new TcpListener(IPAddress.Parse(ipAddress), port);
        listener.Start();
        Console.WriteLine($"Server started on port {port}");

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
                Console.WriteLine($"Received from {message}");

                // Broadcast to all clients
                foreach (var c in clients.Keys)
                {
                    var writer = c.GetStream();
                    await writer.WriteAsync(buffer, 0, bytesRead);
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

    static string GetLocalIPAddress()
    {
        string localIP = string.Empty;

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            var properties = networkInterface.GetIPProperties();
            foreach (var unicastAddress in properties.UnicastAddresses)
            {
                if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = unicastAddress.Address.ToString();
                    break;
                }
            }
            if (!string.IsNullOrEmpty(localIP))
                break;
        }

        return localIP;
    }

    // Get public IP address by querying an external service
    static string GetPublicIPAddress()
    {
        using (var client = new System.Net.WebClient())
        {
            return client.DownloadString("http://checkip.amazonaws.com").Trim();
        }
    }
}
