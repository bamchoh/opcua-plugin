using opcua_plugin.Domain.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Configuration
{
    public class PortValueObject
    {
        public int Value { get; init; }

        public int MinValue => 1;

        public int MaxValue => 65535;

        public PortValueObject(int value)
        {
            if (value < MinValue || value > MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), string.Format("Port number must be between %d and %d.", MinValue, MaxValue));
            }
            Value = value;
        }
    }


    public class OpcUaServerOptions
    {
        public static readonly OpcUaServerOptions Default = new OpcUaServerOptions();

        public string ManufacturerName { get; set; } = "opcua-plugin";
        public string ProductName { get; set; } = "Opc Ua Plugin Server";
        public string ProductUri { get; set; } = "urn:opcua-plugin-server:OpcUaPluginServer:v1.0";
        public string Namespace { get; set; } = "urn:opcua-plugin-server:OpcUaPluginServer:Empty";

        // 起動時に上書きされることを想定
        public string ApplicationName { get; set; } = "SimpleOpcUaServer";
        public PortValueObject Port { get; set; } = new PortValueObject(4840);
    }
}
