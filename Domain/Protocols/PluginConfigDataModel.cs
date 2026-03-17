using opcua_plugin.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Protocols
{
    public class PortValueObjectConverter : JsonConverter<PortValueObject>
    {
        public override PortValueObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new PortValueObject(reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, PortValueObject value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }
    }

    [JsonConverter(typeof(PortValueObjectConverter))]
    public class  PortValueObject
    {
        public int Value { get; init; }

        public int MinValue => 1;

        public int MaxValue => 65535;

        public PortValueObject(int value) {
            if (value < MinValue || value > MaxValue) {
                throw new ArgumentOutOfRangeException(nameof(value), string.Format("Port number must be between %d and %d.", MinValue, MaxValue));
            }
            Value = value;
        }
    }

    public class PluginConfigDataModel
    {
        public static readonly PluginConfigDataModel Default = new PluginConfigDataModel();

        [JsonPropertyName("application_name")]
        public string ApplicationName { get; init; }

        [JsonPropertyName("port")]
        public PortValueObject Port { get; init; }

        public string ProtocolType => "opcua";
        public string DisplayName => "OPC UA";

        public PluginConfigDataModel() {
            ApplicationName = "SimpleOpcUaServer";
            Port = new PortValueObject(4840);
        }
    }
}
