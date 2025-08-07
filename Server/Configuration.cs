using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Server
{
    internal class Configuration
    {
        public string ListenAddr { get; set; } = "0.0.0.0";
        public int ListenPort { get; set; } = 5555;
        public bool TlsEnabled { get; set; } = true;
        public string TlsCrt { get; set; } = string.Empty;
        public string TlsKey { get; set; } = string.Empty;
        public List<Filter> Filters { get; set; } = [];

        public static Configuration LoadFromJson(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Configuration>(json, SourceGenerationContext.Default.Configuration)
                ?? throw new Exception("Invalid configuration.");
        }
    }

    internal class Filter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}