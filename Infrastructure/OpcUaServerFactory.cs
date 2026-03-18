using Microsoft.Extensions.Options;
using opcua_plugin.Domain.Configuration;
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
        private RemoteVariableStoreAccessor _accessor;

        private OpcUaServerOptions _options;

        public OpcUaServerOptions Options { get { return _options; } }

        public string ProtocolType => "opcua";

        public string DisplayName => "OPC UA";

        public OpcUaServerFactory(OpcUaServerOptions options)
        {
            _options = options;
        }

        public ProtocolCapabilitiesModel GetProtocolCapabilities()
        {
            return new ProtocolCapabilitiesModel
            {
                SupportsNodePublishing = true
            };
        }

        public PluginConfigDataModel UpdateConfig(PluginConfigDataModel config)
        {
            var newOptions = MapFromPluginConfig(config);
            _options.ApplicationName = config.ApplicationName;

            _options.Port = new PortValueObject(config.Port);

            _options.ManufacturerName = config.ManufacturerName;

            return config;
        }

        private OpcUaServerOptions MapFromPluginConfig(PluginConfigDataModel cfg)
        {
            var opts = new OpcUaServerOptions
            {
                ApplicationName = cfg.ApplicationName,
                Port = new PortValueObject(cfg.Port),
                ManufacturerName = cfg.ManufacturerName,
                ProductUri = cfg.ProductUri,
                Namespace = cfg.Namespace,
            };

            return opts;
        }

        private PluginConfigDataModel ToPluginConfigModel(OpcUaServerOptions options)
        {
            return new PluginConfigDataModel()
            {
                ApplicationName = options.ApplicationName,
                Port = options.Port.Value,
                ManufacturerName = options.ManufacturerName,
                ProductUri = options.ProductUri,
                Namespace = options.Namespace,
            };
        }

        public PluginConfigDataModel GetDefaultConfig()
        {
            return ToPluginConfigModel(OpcUaServerOptions.Default);
        }

        public PluginConfigDataModel GetConfig()
        {
            return ToPluginConfigModel(_options);
        }


        public List<ConfigFieldModel> GetConfigFields()
        {
            return OpcUaConfigSchema.GetFields(_options).ToList();
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
