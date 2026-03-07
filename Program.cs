using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using opcua_plugin.Infrastructure;
using opcua_plugin.Services;

namespace opcua_plugin
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(System.Net.IPAddress.Loopback, 0, o =>
                {
                    o.Protocols = HttpProtocols.Http2;
                });
            });

            builder.Services.AddGrpc();
            builder.Services.AddSingleton<PluginApplicationService>();
            builder.Services.AddSingleton<OpcUaServerFactory>();

            var app = builder.Build();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                var server = app.Services.GetRequiredService<
                    Microsoft.AspNetCore.Hosting.Server.IServer>();

                var addressesFeature = server.Features.Get<
                    Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();

                if (addressesFeature?.Addresses == null || !addressesFeature.Addresses.Any())
                {
                    throw new InvalidOperationException("Server addresses are not available.");
                }

                foreach (var address in addressesFeature.Addresses)
                {
                    var uri = new Uri(address);
                    Console.WriteLine($"GRPC_PORT={uri.Port}");
                }
            });

            app.MapGrpcService<PluginService>();

            app.Run();
        }
    }
}
