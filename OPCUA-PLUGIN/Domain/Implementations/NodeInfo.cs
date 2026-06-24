using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Implementations
{
    public class JsonNodeInfo
    {
        public string Endpoint { get; set; }
        public string Name { get; set; }
        public List<JsonNodeInfo> Children { get; set; }
        public string Type { get; set; }

        public static NodeInfo CreateNodeTree(JsonNodeInfo jsonNodeInfo)
        {
            if (jsonNodeInfo.Type == null)
            { // Directory
                var node = new Directory();
                node.Name = jsonNodeInfo.Name;
                ((Directory)node).Children = new List<NodeInfo>();
                foreach (var child in jsonNodeInfo.Children)
                {
                    ((Directory)node).Children.Add(CreateNodeTree(child));
                }
                return node;
            }
            else
            {
                var node = new Variable();
                node.Name = jsonNodeInfo.Name;
                node.Type = GetBuiltInType(jsonNodeInfo.Type);
                return node;
            }
        }

        private static BuiltInType GetBuiltInType(string type)
        {
            switch (type.ToLower())
            {
                case "null":
                    return BuiltInType.Null;
                case "boolean":
                    return BuiltInType.Boolean;
                case "sbyte":
                    return BuiltInType.SByte;
                case "byte":
                    return BuiltInType.Byte;
                case "int16":
                    return BuiltInType.Int16;
                case "uint16":
                    return BuiltInType.UInt16;
                case "int32":
                    return BuiltInType.Int32;
                case "uint32":
                    return BuiltInType.UInt32;
                case "int64":
                    return BuiltInType.Int64;
                case "uint64":
                    return BuiltInType.UInt64;
                case "float":
                    return BuiltInType.Float;
                case "double":
                    return BuiltInType.Double;
                case "string":
                    return BuiltInType.String;
                default:
                    return BuiltInType.Null;
            }
        }
    }

    public class NodeInfo
    {
        public string Name { get; set; }
    }

    public class Directory : NodeInfo
    {
        public List<NodeInfo> Children { get; set; }

        public string FullName { get; set; }
    }

    public class Variable : NodeInfo
    {
        public BuiltInType Type { get; set; }
    }
}
