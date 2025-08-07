using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client
{
    internal class Configuration
    {
        public string InputPath { get; set; } = string.Empty;
        public string ServerHost { get; set; } = "localhost";
        public int Server_port { get; set; } = 5555;
        public bool TlsEnabled { get; set; } = true;
        public bool TlsValidateCert { get; set; } = false;

        public static Configuration LoadFromJson(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Configuration>(json)
                ?? throw new Exception("Invalid configuration.");
        }
    }
}
