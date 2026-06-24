using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Protocols
{
    public class CreateAndStartCommand
    {
        public string VariantId { get; init; } = "";
        public string SettingsJson { get; init; } = "";
        public string HostGrpcAddr { get; init; } = "";
    }
}
