using Microsoft.AspNetCore.Hosting.Server;
using opcua_plugin.Domain.Protocols;
using opcua_plugin.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Implementations
{
    public class OpcUaServerManager
    {
        private int portno;

        private string hostname;

        private RemoteVariableStoreAccessor _accessor;

        public string Endpoint => string.Format("opc.tcp://{0}:{1}", hostname, portno);

        private Opc.Ua.Configuration.ApplicationInstance _application;

        private OpcUaProtocolServer _server;

        public ServerStatus Status { get; private set; } = ServerStatus.Stopped;

        public OpcUaServerManager(int port, RemoteVariableStoreAccessor accessor)
        {
            this.portno = port;

            this.hostname = System.Net.Dns.GetHostName();

            this._accessor = accessor;
        }

        public Task StartAsync(CancellationToken cancelToken)
        {
            _application = OpcUaApplication.GetApplicationInstance();
            _application.ApplicationConfiguration.ApplicationName = "SimpleOpcUaServer";
            {
                var ba = _application.ApplicationConfiguration.ServerConfiguration.BaseAddresses;
                ba.Add(Endpoint);
            }

            _server = new OpcUaProtocolServer()
            {
                CancelToken = cancelToken,
                Accessor = _accessor
            };

            var task = _application.StartAsync(_server);

            Status = ServerStatus.Running;

            return task;
        }

        public void Stop()
        {
            if (_application != null)
            {
                _application.StopAsync();
            }

            Status = ServerStatus.Stopped;
        }

        public void OnNodePublishingUpdated()
        {
            if (_server == null || Status != ServerStatus.Running)
            {
                return;
            }
            _server.OnNodePublishingUpdated();
        }
    }
}
