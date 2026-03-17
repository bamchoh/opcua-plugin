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

        private RemoteVariableStoreAccessor _accessor;

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
                    Name = "port",
                    Label = "ポート",
                    Description = "OPC UA サーバーの待ち受けポート番号。標準ポートは 4840 です。",
                    Type = "number",
                    Required = true,
                    Default = defaultConfig.Port.Value,
                    Min = defaultConfig.Port.MinValue,
                    Max = defaultConfig.Port.MaxValue,
                },
                new ConfigFieldModel {
                    Name = "application_name",
                    Label = "アプリケーション名",
                    Description = "OPC UA サーバーで公開されるアプリケーションの名称",
                    Type = "text",
                    Required = true,
                    Default = defaultConfig.ApplicationName,
                }
            };
        }

        public OpcUaServerManager CreateServer()
        {
            return new OpcUaServerManager(_accessor);
        }

        public void InjectVariableStore(RemoteVariableStoreAccessor accessor)
        {
            _accessor = accessor;
        }
    }
}
