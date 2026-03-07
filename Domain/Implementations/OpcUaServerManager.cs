using Microsoft.AspNetCore.Hosting.Server;
using opcua_plugin.Domain.Protocols;
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

        public string Endpoint => string.Format("opc.tcp://{0}:{1}", hostname, portno);

        private Opc.Ua.Configuration.ApplicationInstance _application;

        public ServerStatus Status { get; private set; } = ServerStatus.Stopped;

        public OpcUaServerManager(int port)
        {
            this.portno = port;

            this.hostname = System.Net.Dns.GetHostName();
        }

        public Task StartAsync(CancellationToken cancelToken)
        {
            _application = OpcUaApplication.GetApplicationInstance();
            _application.ApplicationConfiguration.ApplicationName = "SimpleOpcUaServer";
            {
                var ba = _application.ApplicationConfiguration.ServerConfiguration.BaseAddresses;
                ba.Add(Endpoint);
            }

            var server = new OpcUaProtocolServer()
            {
                CancelToken = cancelToken,
            };

            var task = _application.StartAsync(server);

            Status = ServerStatus.Running;

            return task;
        }
    }
}
