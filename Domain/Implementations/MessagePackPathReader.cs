using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Protobuf.Reflection.FeatureSet.Types;

namespace opcua_plugin.Domain.Implementations
{
    public class ParsedNode
    {
        public enum NodeType
        {
            Primitive = 0,
            Map = 1,
            Array = 2,
        };

        public NodeType Type { get; set; } = NodeType.Primitive; // "map", "array"
        public int Index { get; set; } = -1;
        public string Key { get; set; } = "";
    }

    public class MessagePackPathReader
    {
        public static bool ParseNodeIdentifier(string identifier, ref List<ParsedNode> result)
        {
            // identifier から構造を抽出する。Struct1[0].Field1 などの形式を想定
            var idx = -1;
            var delimiters = new[] { '[', '.' };
            if ((idx = identifier.IndexOfAny(delimiters)) >= 0)
            {
                if (identifier[idx] == '[')
                {
                    // 配列の形式
                    int arrayStart = idx;
                    int arrayEnd = identifier.IndexOf(']');
                    string indexPart = identifier.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    result.Add(new ParsedNode { Index = Int32.Parse(indexPart), Type = ParsedNode.NodeType.Array });
                    if (arrayEnd + 1 < identifier.Length)
                    {
                        // 配列の後にさらにフィールドが続く場合
                        var name = identifier.Substring(0, arrayStart);
                        if (!ParseNodeIdentifier(name + identifier.Substring(arrayEnd + 1), ref result))
                            return false;
                    }
                }
                else
                {
                    var key = identifier.Substring(idx + 1);
                    if ((idx = key.IndexOfAny(delimiters)) >= 0)
                    {
                        result.Add(new ParsedNode { Key = key.Substring(0, idx), Type = ParsedNode.NodeType.Map });

                        if (!ParseNodeIdentifier(key.Substring(idx), ref result))
                            return false;
                    }
                    else
                    {
                        result.Add(new ParsedNode { Key = key, Type = ParsedNode.NodeType.Map });
                    }
                }
            }
            else
            {
                // プリミティブ変数
                result.Add(new ParsedNode { });
            }

            return true;
        }

        public static bool TryGetMsgPackValue(byte[] msgpackbin, List<ParsedNode> parsedNodes, string elemType, int lowerBound, out object? val)
        {
            var reader = new MessagePackReader(msgpackbin);
            for (int i = 0; i < parsedNodes.Count; i++)
            {
                switch (parsedNodes[i].Type)
                {
                    case ParsedNode.NodeType.Array:
                        {
                            var idx = parsedNodes[i].Index - lowerBound;
                            var count = reader.ReadArrayHeader();

                            if (idx >= count)
                            {
                                val = null;
                                return false;
                            }

                            for (int j = 0; j < idx; j++)
                            {
                                reader.Skip();
                            }
                        }
                        break;
                    case ParsedNode.NodeType.Map:
                        {
                            var count = reader.ReadMapHeader();
                            for (int j = 0; j < count; j++)
                            {
                                var key = reader.ReadString();
                                if (key == parsedNodes[i].Key)
                                    break;

                                reader.Skip();
                            }
                        }
                        break;
                    default:
                        {
                            (val, bool ret) = ParseMessagePackValue(ref reader, elemType);
                            return ret;
                        }
                }
            }

            {
                (val, bool ret) = ParseMessagePackValue(ref reader, elemType);
                return ret;
            }
        }

        private static (object?, bool) ReadValueByType(ref MessagePackReader reader, string elemType)
        {
            switch (elemType.ToUpper())
            {
                case "BOOL":
                    return (reader.ReadBoolean(), true);
                case "SINT":
                    return (reader.ReadSByte(), true);
                case "USINT":
                    return (reader.ReadByte(), true);
                case "INT":
                    return (reader.ReadInt16(), true);
                case "UINT":
                    return (reader.ReadUInt16(), true);
                case "DINT":
                case "TIME":
                    return (reader.ReadInt32(), true);
                case "UDINT":
                    return (reader.ReadUInt32(), true);
                case "LINT":
                    return (reader.ReadInt64(), true);
                case "ULINT":
                    return (reader.ReadUInt64(), true);
                case "REAL":
                    return (reader.ReadSingle(), true);
                case "LREAL":
                    return (reader.ReadDouble(), true);
                case "STRING":
                    return (reader.ReadString(), true);
                case "DATE":
                case "TIME_OF_DAY":
                case "DATE_AND_TIME":
                    return (reader.ReadDateTime(), true);
                default:
                    {
                        return ParseMessagePackValue(ref reader, elemType);
                    }
            }
        }

        private static (object?, bool) ParseMessagePackValue(ref MessagePackReader reader, string elemType)
        {
            switch(reader.NextMessagePackType)
            {
                case MessagePackType.Array:
                    {
                        var count = reader.ReadArrayHeader();
                        var list = new List<object?>();
                        for (int i = 0; i < count; i++)
                        {
                            (var elem, bool ret) = ParseMessagePackValue(ref reader, elemType);
                            if (!ret)
                            {
                                return (null, ret);
                            }
                            list.Add(elem);
                        }
                        return (list.ToArray(), true);
                    }
                case MessagePackType.Boolean:
                case MessagePackType.Integer:
                case MessagePackType.Float:
                case MessagePackType.String:
                    return ReadValueByType(ref reader, elemType);
                default:
                    {
                        return (null, true);
                    }
            }
        }
    }
}
