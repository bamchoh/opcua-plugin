using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Protocols
{
    public class ProtocolCapabilitiesModel
    {
        public bool SupportsUnitId { get; init; }
        public int UnitIdMin { get; init; }
        public int UnitIdMax { get; init; }
        public bool SupportsNodePublishing { get; init; }
    }
}
