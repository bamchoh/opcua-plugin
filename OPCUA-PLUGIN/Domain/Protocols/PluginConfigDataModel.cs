using opcua_plugin.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using opcua_plugin.Domain.Configuration;

namespace opcua_plugin.Domain.Protocols
{
    public class PluginConfigDataModel
    {
        [JsonPropertyName(PluginConfigKeys.ApplicationName)]
        public string ApplicationName { get; init; }

        [JsonPropertyName(PluginConfigKeys.Port)]
        public int Port { get; init; }

        [JsonPropertyName(PluginConfigKeys.ManufacturerName)]
        public string ManufacturerName { get; init; }

        [JsonPropertyName(PluginConfigKeys.ProductUri)]
        public string ProductUri { get; init;  }

        [JsonPropertyName(PluginConfigKeys.Namespace)]
        public string Namespace { get; init; }

        public PluginConfigDataModel() { }
    }
}
