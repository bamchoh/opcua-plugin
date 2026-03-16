using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Protocols
{
    public class FieldOption
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";
    }

    public class FieldCondition
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = "";
        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
    }

    public class ConfigFieldModel
	{
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text"; // "text", "number", "select"

		[JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("default")]
        public object Default { get; set; } = "";

		[JsonPropertyName("options")]
        public List<FieldOption>? Options { get; set; }

        [JsonPropertyName("min")]
        public int? Min { get; set; }

        [JsonPropertyName("max")]
        public int? Max { get; set; }

        [JsonPropertyName("condition")]
        public FieldCondition? Condition { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
