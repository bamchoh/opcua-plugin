using Google.Protobuf;
using Grpc.Core;
using Plugin.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Plugin.V1.VariableAccessorService;

namespace opcua_plugin.Infrastructure
{
    public class NodePublishingInfo
    {
        public string VariableId { get; set; }
        public string VariableName { get; set; }
        public string DataType { get; set; }
        public string AccessMode { get; set; }
    }

    public class StructFieldInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
    }

    public class RemoteVariableStoreAccessor
    {
        private readonly VariableAccessorServiceClient _client;
        public RemoteVariableStoreAccessor(VariableAccessorServiceClient client)
        {
            _client = client;
        }

        public List<NodePublishingInfo> GetEnabledNodePublishings(string nodeId)
        {
            var resp = _client.GetEnabledNodePublishings(new GetNodePublishingsRequest() { ProtocolType = "opcua-cs" });
            var result = new List<NodePublishingInfo>();
            foreach (var publishing in resp.Publishings)
            {
                result.Add(new NodePublishingInfo()
                {
                    VariableId = publishing.VariableId,
                    VariableName = publishing.VariableName,
                    DataType = publishing.DataType,
                    AccessMode = publishing.AccessMode
                });
            }
            return result;
        }

        public (byte[] bytes, string error) ReadVariableValue(string variableId)
        {
            var resp = _client.ReadVariableValue(new ReadVariableValueRequest { VariableId = variableId });
            return (resp.ValueMsgpack.ToByteArray(), resp.Error);
        }

        public void WriteVariableValue(string variableId, byte[] valueBytes)
        {
            _client.WriteVariableValue(new WriteVariableValueRequest { VariableId = variableId, ValueMsgpack = ByteString.CopyFrom(valueBytes) });
        }

        public void WriteVariableField(string variableId, string fieldPath, byte[] valueBytes)
        {
            _client.WriteVariableField(new WriteVariableFieldRequest { VariableId = variableId, FieldPath = fieldPath, ValueMsgpack = ByteString.CopyFrom(valueBytes) });
        }

        public AsyncServerStreamingCall<VariableChange> SubscribeVariableChanges()
        {
            return _client.SubscribeVariableChanges(new Empty());
        }

        public List<StructFieldInfo> GetStructFields(string dataType)
        {
            var resp = _client.GetStructFields(new GetStructFieldsRequest() { TypeName = dataType });
            var result = new List<StructFieldInfo>();
            foreach (var field in resp.Fields)
            {
                result.Add(new StructFieldInfo()
                {
                    Name = field.Name,
                    DataType = field.DataType
                });
            }
            return result;
        }
    }
}
