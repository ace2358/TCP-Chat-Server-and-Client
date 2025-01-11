using System;
using System.IO;
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

        var client = new TcpClient();
        await client.ConnectAsync(server, port);
        Console.WriteLine("Connected to server.");
        Console.WriteLine("Enter a message || Enter 'file_upload' to upload a file");

        var stream = client.GetStream();

        // Send the name as the first message
        var nameBuffer = Encoding.UTF8.GetBytes(name);
        await stream.WriteAsync(nameBuffer, 0, nameBuffer.Length);

        // Sending messages in a separate task
        _ = Task.Run(async () =>
        {
            while (true)
            {
                string message = Console.ReadLine();
                if (message.Equals("file_upload", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write("Enter the file path to upload: ");
                    string filePath = Console.ReadLine();

                    if (File.Exists(filePath))
                    {
                        var uploadBuffer = Encoding.UTF8.GetBytes("file_upload");
                        await stream.WriteAsync(uploadBuffer, 0, uploadBuffer.Length);

                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            Console.WriteLine("Uploading file...");
                            await fileStream.CopyToAsync(stream);
                        }
                        Console.WriteLine("File upload completed.");
                    }
                    else
                    {
                        Console.WriteLine("File not found. Try again.");
                    }
                }
                else
                {
                    var buffer = Encoding.UTF8.GetBytes($"{name}: {message}");
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        });

        // Receiving messages
        var receiveBuffer = new byte[1024];
        while (true)
        {
            int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
            string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
            Console.WriteLine(receivedMessage);
        }
    }
}
