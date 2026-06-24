using opcua_plugin.Domain.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Configuration
{
    public static class OpcUaConfigSchema
    {
        public static IReadOnlyList<ConfigFieldModel> GetFields(OpcUaServerOptions options)
        {
            var opts = options ?? OpcUaServerOptions.Default;

            return new List<ConfigFieldModel> {
                new ConfigFieldModel {
                    Name = PluginConfigKeys.Port,
                    Label = "ポート",
                    Description = "OPC UA サーバーの待ち受けポート番号。標準ポートは 4840 です。",
                    Type = "number",
                    Required = true,
                    Default = options.Port.Value,
                    Min = options.Port.MinValue,
                    Max = options.Port.MaxValue,
                },
                new ConfigFieldModel {
                    Name = PluginConfigKeys.ApplicationName,
                    Label = "アプリケーション名 (ApplicationName)",
                    Description = "OPC UA サーバーで公開されるアプリケーションの名称",
                    Type = "text",
                    Required = true,
                    Default = options.ApplicationName,
                },
                new ConfigFieldModel {
                    Name = PluginConfigKeys.ManufacturerName,
                    Label = "メーカー名 (ManufacturerName)",
                    Description = "OPC UA サーバーで公開されるメーカー名",
                    Type = "text",
                    Required = true,
                    Default = options.ManufacturerName,
                },
                new ConfigFieldModel {
                    Name = PluginConfigKeys.ProductUri,
                    Label = "プロダクトURI (ProductUri)",
                    Description = "OPC UA サーバーで公開されるプロダクトURI",
                    Type = "text",
                    Required = true,
                    Default = options.ProductUri,
                },
                new ConfigFieldModel {
                    Name = PluginConfigKeys.Namespace,
                    Label = "ネームスペース (Namespace)",
                    Description = "OPC UA サーバーで公開されるネームスペース",
                    Type = "text",
                    Required = true,
                    Default = options.Namespace,
                },
            };
        }

    }
}
