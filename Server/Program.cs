using System.Collections.Generic;
using System.IO;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
                        try
                        {
                            if (ApplyFilters(line, config.Filters))
                            {
                                var prettyJson = JToken.Parse(line).ToString(Formatting.Indented);
                                Console.WriteLine(prettyJson);
                            }
                        }
                        catch (JsonReaderException)
                        {
                            Console.Error.WriteLine($"Invalid JSON: {line}");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Client error: {ex.Message}");
            }
        }

        private static bool ApplyFilters(string jsonString, List<Filter> filters)
        {
            var jsonObject = JObject.Parse(jsonString);
            foreach (var filter in filters)
            {
                var fieldValue = jsonObject[filter.Field]?.ToString();
                switch (filter.Operator)
                {
                    case "equals":
                        if (fieldValue != filter.Value) return false;
                        break;
                    case "not_equals":
                        if (fieldValue == filter.Value) return false;
                        break;
                    case "contains":
                        if (fieldValue == null || !fieldValue.Contains(filter.Value)) return false;
                        break;
                    case "not_contains":
                        if (fieldValue != null && fieldValue.Contains(filter.Value)) return false;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown operator: {filter.Operator}");
                }
            }
            return true;
        }

    }
}