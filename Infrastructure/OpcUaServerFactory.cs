using opcua_plugin.Domain.Implementations;
using opcua_plugin.Domain.Protocols;
using Plugin.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Infrastructure
{
    public class OpcUaServerFactory
    {
        private PluginConfigDataModel _config;

        public string ProtocolType => _config.ProtocolType;

        public string DisplayName => _config.DisplayName;

        public OpcUaServerFactory()
        {
            _config = PluginConfigDataModel.Default;
        }

        public ProtocolCapabilitiesModel GetProtocolCapabilities()
        {
            return new ProtocolCapabilitiesModel
            {
                SupportsNodePublishing = true
            };
        }

        public PluginConfigDataModel GetDefaultConfig()
        {
            return PluginConfigDataModel.Default;
        }

        public PluginConfigDataModel GetConfig()
        {
            return _config;
        }

        public PluginConfigDataModel UpdateConfig(PluginConfigDataModel config)
        {
            _config = config;
            return _config;
        }

        public List<ConfigFieldModel> GetConfigFields()
        {
            var defaultConfig = GetDefaultConfig();

            return new List<ConfigFieldModel> {
                new ConfigFieldModel {
                    Name = "host",
                    Label = "ホスト (0.0.0.0で全インターフェース)",
                    Type = "text",
                    Required = true,
                    Default = defaultConfig.Host,
                },
                new ConfigFieldModel {
                    Name = "port",
                    Label = "ポート",
                    Type = "number",
                    Required = true,
                    Default = defaultConfig.Port.Value,
                    Min = defaultConfig.Port.MinValue,
                    Max = defaultConfig.Port.MaxValue,
                }
            };
        }

        public OpcUaServerManager CreateServer(PluginConfigDataModel config)
        {
            var port = config.Port.Value;
            return new OpcUaServerManager(port);
        }
    }
}
