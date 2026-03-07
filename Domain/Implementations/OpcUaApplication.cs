using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Implementations
{
    public static class OpcUaApplication
    {
        static string GenerateConfigurationFile()
        {
            var name = Path.GetTempFileName();

            using (var writer = new FileStream(name, FileMode.Create))
            using (var streamWriter = new StreamWriter(writer))
            {
                streamWriter.Write("Configuration.xml");
            }

            return name;
        }

        public static ApplicationInstance GetApplicationInstance()
        {
            var telemetryContext = DefaultTelemetry.Create(builder => builder.AddConsole());
            var application = new ApplicationInstance(telemetryContext)
            {
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "OpcUaServer"
            };
            var conffile = Path.Combine(AppContext.BaseDirectory, "Configuration.xml");
            application.LoadApplicationConfigurationAsync(conffile, false).AsTask().Wait();
            application.CheckApplicationInstanceCertificatesAsync(false, 0).AsTask().Wait();
            return application;
        }
    }
}
