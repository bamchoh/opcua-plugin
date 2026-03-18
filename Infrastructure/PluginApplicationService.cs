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
using opcua_plugin.Domain.Configuration;

namespace opcua_plugin.Infrastructure
{
    public class PluginApplicationService
    {
        private readonly OpcUaServerFactory _factory;

        private OpcUaServerManager _server;

        private GrpcChannel _channel;

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

        public List<ConfigFieldModel> GetConfigFields(string variantId)
        {
            return _factory.GetConfigFields();
        }

        public PluginConfigDataModel UpdateConfig(PluginConfigDataModel config)
        {
            return _factory.UpdateConfig(config);
        }

        public async Task CreateAndStartAsync(CreateAndStartCommand cmd)
        {
            if (!string.IsNullOrEmpty(cmd.HostGrpcAddr))
            {
                Console.WriteLine($"Creating gRPC channel to {cmd.HostGrpcAddr}");
                _channel = GrpcChannel.ForAddress(string.Format("http://{0}", cmd.HostGrpcAddr));
                var client = new VariableAccessorService.VariableAccessorServiceClient(_channel);
                var accessor = new RemoteVariableStoreAccessor(client);
                _factory.InjectVariableStore(accessor);
            }

            PluginConfigDataModel config;

            // 設定の復元
            if (!string.IsNullOrEmpty(cmd.SettingsJson))
            {
                var _config = JsonSerializer.Deserialize<PluginConfigDataModel>(cmd.SettingsJson);

                if (_config == null)
                {
                    throw new RpcException(
                        new Status(StatusCode.InvalidArgument, "Invalid SettingsJson"));
                }

                _factory.UpdateConfig(_config);
            }

            // var dataStore = _factory.CreateDataStore();

            var server = _factory.CreateServer();

            _server = server;

            var cts = new CancellationTokenSource();
            await server.StartAsync(cts.Token, _factory.Options);
        }

        public void Stop()
        {
            if(_server != null)
                _server.Stop();

            if (_channel != null)
                _channel.Dispose();
        }

        public string GetStatus()
        {
            if(_server == null)
            {
                return ServerStatus.Stopped.ToString();
            }
            return _server.Status.ToString();
        }

        public void OnNodePublishingUpdated()
        {
            _server.OnNodePublishingUpdated();
        }
    }
}
