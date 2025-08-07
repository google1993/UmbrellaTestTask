using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: Client <config-file>");
                Environment.Exit(1);
            }

            try
            {
                var config = Configuration.LoadFromJson(args[0]);
                await RunClient(config);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static async Task RunClient(Configuration config)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(config.ServerHost, config.ServerPort);

            Stream stream = client.GetStream();
            if (config.TlsEnabled)
            {
                var sslStream = new SslStream(stream,
                    false,
                    (sender, cert, chain, errors) => !config.TlsValidateCert || errors == SslPolicyErrors.None,
                    null);
                await sslStream.AuthenticateAsClientAsync(config.ServerHost);
                stream = sslStream;
            }

            using var writer = new StreamWriter(stream, leaveOpen: true);
            using var fileReader = new StreamReader(config.InputPath);
            string? line;
            while ((line = await fileReader.ReadLineAsync()) != null)
            {
                if (IsValidJson(line))
                {
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                }
                else
                {
                    Console.Error.WriteLine($"Invalid JSON: {line}");
                }
            }
        }

        private static bool IsValidJson(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

    }
}