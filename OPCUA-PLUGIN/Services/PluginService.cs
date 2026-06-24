using Google.Protobuf.Reflection;
using Grpc.Core;
using opcua_plugin.Infrastructure;
using Plugin.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace opcua_plugin.Services
{
    public class DataStoreSerivce : Plugin.V1.DataStoreService.DataStoreServiceBase
    {
        public override Task SubscribeChanges(Empty request, IServerStreamWriter<DataChange> responseStream, ServerCallContext context)
        {
            // OPC UA は DataStore の変更通知を持たないため、クライアントが切断するまで待つ
            return Task.Run(async () =>
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000); // 1秒ごとにキャンセルを確認
                }
            });
        }

        public override Task<GetAreasResponse> GetAreas(Empty request, ServerCallContext context)
        {
            // OPC UA は DataStore のエリア概念を持たないため、空のレスポンスを返す
            return Task.FromResult(new GetAreasResponse());
        }
    }

    public class PluginService : Plugin.V1.PluginService.PluginServiceBase
    {
        private readonly PluginApplicationService _app;

        public PluginService(PluginApplicationService app)
        {
            _app = app;
        }

        public override Task<PluginMetadata> GetMetadata(Empty request, ServerCallContext context)
        {
            var metadata = _app.GetMetaData();

            return Task.FromResult(new PluginMetadata
            {
                ProtocolType = metadata.ProtocolType,
                DisplayName = metadata.DisplayName,
                Capabilities = new ProtocolCapabilities
                {
                    SupportsUnitId = false,
                    UnitIdMin = 0,
                    UnitIdMax = 0,
                    SupportsNodePublishing = metadata.Capabilities.SupportsNodePublishing
                }
            });
        }

        public override Task<GetConfigFieldsResponse> GetConfigFields(GetConfigFieldsRequest request, ServerCallContext context)
        {
            var configFields = _app.GetConfigFields(request.VariantId);

            var configFieldsResponse = new GetConfigFieldsResponse();

            foreach (var field in configFields)
            {
                configFieldsResponse.Fields.Add(new ConfigField
                {
                    Name = field.Name,
                    Label = field.Label,
                    Type = field.Type,
                    Description = field.Description,
                    Required = field.Required,
                    DefaultJson = JsonSerializer.Serialize(field.Default),
                    HasMin = field.Min == null,
                    Min = field.Min ?? 0,
                    HasMax = field.Max == null,
                    Max = field.Max ?? 0
                });
            }

            return Task.FromResult(configFieldsResponse);
        }

        public override Task<ConfigDataResponse> GetDefaultConfig(GetDefaultConfigRequest request, ServerCallContext context)
        {
            var config = _app.GetDefaultConfig(request.VariantId);

            return Task.FromResult(new ConfigDataResponse
            {
                VariantId = request.VariantId,
                SettingsJson = JsonSerializer.Serialize(config),
            });
        }

        public override Task<ConfigToMapResponse> ConfigToMap(ConfigToMapRequest request, ServerCallContext context)
        {
            var config = _app.GetConfig(request.VariantId);

            // ここでは特に変換は行わず、JSONをそのまま返す
            return Task.FromResult(new ConfigToMapResponse
            {
                SettingsJson = JsonSerializer.Serialize(config)
            });
        }

        public override Task<MapToConfigResponse> MapToConfig(MapToConfigRequest request, ServerCallContext context)
        {
            var config = JsonSerializer.Deserialize<Domain.Protocols.PluginConfigDataModel>(request.SettingsJson);

            if (config == null)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid SettingsJson"));
            }

            config = _app.UpdateConfig(config);

            return Task.FromResult(new MapToConfigResponse
            {
                SettingsJson = JsonSerializer.Serialize(config)
            });
        }

        public override Task<StatusResponse> GetStatus(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new StatusResponse
            {
                Status = _app.GetStatus(),
                ErrorMessage = ""
            });
        }

        public override async Task<Empty> CreateAndStart(CreateAndStartRequest request, ServerCallContext context)
        {
            await _app.CreateAndStartAsync(new Domain.Protocols.CreateAndStartCommand()
            {
                VariantId = request.VariantId,
                SettingsJson = request.SettingsJson,
                HostGrpcAddr = request.HostGrpcAddr,
            });

            return new Empty();
        }

        public override Task<Empty> Stop(Empty request, ServerCallContext context)
        {
            _app.Stop();
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> OnNodePublishingUpdated(Empty request, ServerCallContext context)
        {
            _app.OnNodePublishingUpdated();
            return Task.FromResult(new Empty());
        }
    }
}
    