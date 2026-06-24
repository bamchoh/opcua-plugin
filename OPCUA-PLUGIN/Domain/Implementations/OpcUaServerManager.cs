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
        private RemoteVariableStoreAccessor _accessor;

        private Opc.Ua.Configuration.ApplicationInstance _application;

        private OpcUaProtocolServer _server;

        public ServerStatus Status { get; private set; } = ServerStatus.Stopped;

        public OpcUaServerManager(RemoteVariableStoreAccessor accessor)
        {
            this._accessor = accessor;
        }

        public Task StartAsync(CancellationToken cancelToken, Domain.Configuration.OpcUaServerOptions options)
        {
            _application = OpcUaApplication.GetApplicationInstance();
            _application.ApplicationConfiguration.ApplicationName = options.ApplicationName;
            {
                var ba = _application.ApplicationConfiguration.ServerConfiguration.BaseAddresses;

                var hostname = System.Net.Dns.GetHostName();
                var endpoint = string.Format("opc.tcp://{0}:{1}", hostname, options.Port.Value);

                ba.Add(endpoint);
            }

            _server = new OpcUaProtocolServer(options, _accessor, cancelToken);

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
