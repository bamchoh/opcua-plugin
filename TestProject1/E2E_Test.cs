using Grpc.Core;
using MessagePack;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Bindings;
using opcua_plugin.Domain.Implementations;
using opcua_plugin.Infrastructure;
using OpcUaClient;
using Plugin.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TestProject1
{
    public class StubRemoteVariableStoreAccessor : IRemoteVariableStoreAccessor
    {
        public StubRemoteVariableStoreAccessor() { }

        public List<opcua_plugin.Infrastructure.NodePublishingInfo> GetEnabledNodePublishings(string nodeId)
        {
            var list = new List<opcua_plugin.Infrastructure.NodePublishingInfo>();

            list.Add(new opcua_plugin.Infrastructure.NodePublishingInfo()
            {
                VariableId = "id1",
                VariableName = "Var1",
                DataType = "INT",
                AccessMode = "rw",
            });

            return list;
        }

        public List<opcua_plugin.Infrastructure.StructFieldInfo> GetStructFields(string dataType)
        {
            throw new NotImplementedException();
        }

        public (byte[] bytes, string error) ReadVariableValue(string variableId)
        {
            byte[] bytes = MessagePackSerializer.Serialize(new int[] {1,2,3,4,5});
            return (bytes, "");
        }

        public AsyncServerStreamingCall<VariableChange> SubscribeVariableChanges()
        {
            throw new NotImplementedException();
        }

        public void WriteVariableField(string variableId, string fieldPath, byte[] valueBytes)
        {
            throw new NotImplementedException();
        }

        public void WriteVariableValue(string variableId, byte[] valueBytes)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public sealed class E2E_Test
    {
        [TestMethod]
        public void TestMethod1()
        {
            var cts = new CancellationTokenSource();
            var accessor = new StubRemoteVariableStoreAccessor();
            var _application = OpcUaApplication.GetApplicationInstance();
            _application.ApplicationConfiguration.ServerConfiguration.BaseAddresses.Add("opc.tcp://localhost:48010");
            var _server = new OpcUaProtocolServer(null, accessor, cts.Token);
            var task = _application.StartAsync(_server);

            TestOpcUaClient.Connect().Wait();
        }
    }
}