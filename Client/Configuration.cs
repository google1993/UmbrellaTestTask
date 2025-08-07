using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Client
{
    internal class Configuration
    {
        public string InputPath { get; set; } = string.Empty;
        public string ServerHost { get; set; } = "localhost";
        [JsonPropertyName("Server_port")]
        public int ServerPort { get; set; } = 5555;
        public bool TlsEnabled { get; set; } = true;
        public bool TlsValidateCert { get; set; } = false;

        public static Configuration LoadFromJson(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Configuration>(json, SourceGenerationContext.Default.Configuration)
                ?? throw new Exception("Invalid configuration.");
        }
    }
}
