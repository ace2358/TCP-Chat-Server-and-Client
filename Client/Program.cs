using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string server = "127.0.0.1";
        int port = 5000;

        Console.Write("Enter your name: ");
        string name = Console.ReadLine();

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(server, port);
            Console.WriteLine("Connected to server.");

            var stream = client.GetStream();

            // Send the name as the first message
            var nameBuffer = Encoding.UTF8.GetBytes(name);
            await stream.WriteAsync(nameBuffer, 0, nameBuffer.Length);

            // Sending messages in a separate task
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        string message = Console.ReadLine();
                        var buffer = Encoding.UTF8.GetBytes($"{name}: {message}");
                        await stream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while sending message: {ex.Message}");
                        break;
                    }
                }
            });

            // Receiving messages
            var receiveBuffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead == 0) throw new SocketException(); // Trigger disconnection handling
                    string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    Console.WriteLine(receivedMessage);
                }
                catch (Exception)
                {
                    Console.WriteLine("Server has disconnected.");
                    break;
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Could not connect to the server. Please try again later.");
        }
    }
}
