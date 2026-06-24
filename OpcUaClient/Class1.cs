using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace OpcUaClient
{
    public class TestOpcUaClient
    {
        public static async Task Connect()
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = "MyClient",
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true
                },

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000
                },

                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60000
                }
            };

            // await config.ValidateAsync(ApplicationType.Client);

            var telemetryContext = DefaultTelemetry.Create(builder => builder.ClearProviders());

            try
            {
                var endpoint = await CoreClientUtils.SelectEndpointAsync(
                    config,
                    "opc.tcp://localhost:48010",
                    false,
                    telemetryContext);

                var configuredEndpoint = new ConfiguredEndpoint(
                    null,
                    endpoint,
                    EndpointConfiguration.Create(config));

                var factory = new DefaultSessionFactory(null);

                ISession session =
                    await factory.CreateAsync(
                        config,
                        configuredEndpoint,
                        false,
                        "TestSession",
                        60000,
                        null,
                        null);

                var item = new ReadValueId
                {
                    NodeId = "ns=2;s=Var1",
                    AttributeId = Attributes.Value,
                    IndexRange = "2"
                };

                var nodesToRead = new ReadValueIdCollection
                {
                    item
                };

                var response = await session.ReadAsync(
                    null,
                    0,
                    TimestampsToReturn.Neither,
                    nodesToRead,
                    CancellationToken.None);

                var value = response.Results[0];

                Console.WriteLine(value.Value);
                Console.WriteLine(value.StatusCode);
                Console.WriteLine(value.SourceTimestamp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
