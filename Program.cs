using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using opcua_plugin.Domain.Configuration;
using opcua_plugin.Infrastructure;
using opcua_plugin.Services;
using System.Text.Json;

namespace opcua_plugin
{

    internal class Program
    {
        class PluginConfig
        {
            public int? debug_port { get; set; }
        }

        static int GetGrpcPort()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "plugin.json");

            if (!File.Exists(path))
                return 0;

            var json = File.ReadAllText(path);

            var config = JsonSerializer.Deserialize<PluginConfig>(json);

            return config?.debug_port ?? 0;
        }

        static void Main(string[] args)
        {
            var grpcPort = GetGrpcPort();

            var builder = WebApplication.CreateSlimBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(System.Net.IPAddress.Loopback, grpcPort, o =>
                {
                    o.Protocols = HttpProtocols.Http2;
                });
            });

            builder.Services.AddGrpc();
            builder.Services.AddSingleton<PluginApplicationService>();
            builder.Services.AddSingleton<OpcUaServerFactory>();
            builder.Services.AddSingleton<OpcUaServerOptions>();

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
