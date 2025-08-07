using System.Collections.Generic;
using System.IO;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server
{

    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: Server <config-file>");
                Environment.Exit(1);
            }

            try
            {
                var config = Configuration.LoadFromJson(args[0]);
                await RunServer(config);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Server error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static async Task RunServer(Configuration config)
        {
            var listener = new TcpListener(IPAddress.Parse(config.ListenAddr), config.ListenPort);
            listener.Start();
            Console.WriteLine($"Listening on {config.ListenAddr}:{config.ListenPort}");

            X509Certificate2? certificate = null;
            if (config.TlsEnabled)
            {
                certificate = X509Certificate2.CreateFromPemFile(config.TlsCrt, config.TlsKey);
            }

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, config, certificate);
            }
        }

        private static async Task HandleClientAsync(
            TcpClient tcpClient,
            Configuration config,
            X509Certificate2? certificate)
        {
            try
            {
                using (tcpClient)
                {
                    Stream stream = tcpClient.GetStream();

                    if (config.TlsEnabled && certificate != null)
                    {
                        var sslStream = new SslStream(stream, false);
                        await sslStream.AuthenticateAsServerAsync(certificate);
                        stream = sslStream;
                    }

                    using var reader = new StreamReader(stream);
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!IsValidJson(line))
                        {
                            Console.Error.WriteLine($"Invalid JSON: {line}");
                            continue;
                        }

                        using JsonDocument doc = JsonDocument.Parse(line);
                        if (ApplyFilters(doc.RootElement, config.Filters))
                        {
                            var options = new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                TypeInfoResolver = SourceGenerationContext.Default
                            };
                             //Can't fix. Try: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
                            string prettyJson = JsonSerializer.Serialize(doc, options);
                            Console.WriteLine(prettyJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Client error: {ex.Message}");
            }
        }

        private static bool ApplyFilters(JsonElement root, List<Filter> filters)
        {
            foreach (var filter in filters)
            {
                if (!root.TryGetProperty(filter.Field, out var field))
                {
                    return false;
                }

                string fieldValue = field.ValueKind switch
                {
                    JsonValueKind.String => field.GetString() ?? "",
                    JsonValueKind.Null => "null",
                    _ => field.ToString()
                };

                switch (filter.Operator)
                {
                    case "equals":
                        if (fieldValue != filter.Value) return false;
                        break;
                    case "not_equals":
                        if (fieldValue == filter.Value) return false;
                        break;
                    case "contains":
                        if (!fieldValue.Contains(filter.Value)) return false;
                        break;
                    case "not_contains":
                        if (fieldValue.Contains(filter.Value)) return false;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown operator: {filter.Operator}");
                }
            }
            return true;
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