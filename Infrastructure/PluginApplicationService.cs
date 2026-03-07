using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting.Server;
using opcua_plugin.Domain.Protocols;
using Plugin.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using opcua_plugin.Domain.Implementations;

namespace opcua_plugin.Infrastructure
{
    public class PluginApplicationService
    {
        private readonly OpcUaServerFactory _factory;

        private OpcUaServerManager _server;

        public PluginApplicationService(OpcUaServerFactory factory)
        {
            Console.WriteLine("PluginApplicationService created");

            this._factory = factory;
        }

        public PluginMetadataModel GetMetaData()
        {
            var metadata = new PluginMetadataModel
            {
                ProtocolType = _factory.ProtocolType,
                DisplayName = _factory.DisplayName,
                Capabilities = _factory.GetProtocolCapabilities()
            };
            return metadata;
        }

        public PluginConfigDataModel GetDefaultConfig(string varianId)
        {
            return _factory.GetDefaultConfig();
        }

        public PluginConfigDataModel GetConfig(string variantId)
        {
            return _factory.GetConfig();
        }

        public PluginConfigDataModel UpdateConfig(PluginConfigDataModel config)
        {
            return _factory.UpdateConfig(config);
        }

        public List<ConfigFieldModel> GetConfigFields(string variantId)
        {
            return _factory.GetConfigFields();
        }

        public async Task CreateAndStartAsync(CreateAndStartCommand cmd)
        {
            // TODO: VariableStoreアクセサの設定もする必要あり

            PluginConfigDataModel config;

            // 設定の復元
            if (!string.IsNullOrEmpty(cmd.SettingsJson))
            {
                config = _factory.GetDefaultConfig();
            }
            else
            {
                var _config = JsonSerializer.Deserialize<PluginConfigDataModel>(cmd.SettingsJson);

                if (_config == null)
                {
                    throw new RpcException(
                        new Status(StatusCode.InvalidArgument, "Invalid SettingsJson"));
                }

                config = _factory.UpdateConfig(_config);
            }

            // var dataStore = _factory.CreateDataStore();

            var server = _factory.CreateServer(config);

            _server = server;

            var cts = new CancellationTokenSource();
            await server.StartAsync(cts.Token);
        }

        public string GetStatus()
        {
            if(_server == null)
            {
                return ServerStatus.Stopped.ToString();
            }
            return _server.Status.ToString();
        }
    }
}
