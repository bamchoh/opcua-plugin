using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Protocols
{
    public class PluginMetadataModel
    {
        public string ProtocolType { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public ProtocolCapabilitiesModel Capabilities { get; init; } = new();
    }
}
