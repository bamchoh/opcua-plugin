using Opc.Ua;
using opcua_plugin.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Implementations
{
    public class PlcVariableInfo
    {
        public string NodeId { get; set; }
		public string Name { get; set; }
		public string AccessMode { get; set; } // "read" | "write" | "readwrite"
		public BuiltInType DataType
        {
            get
            {
                try
                {
                    return ConvertType(DataTypeString);
                }
                catch
                {
                    return BuiltInType.Null;
                }
            }
        }

        public bool IsArray { get; set; }
        public int LowerBound { get; set; } // 配列の下限（ARRAY[0..9] の場合は0)
        public string SubArrayType { get; set; } // 多次元配列の場合の次の次元の型（"ARRAY[2..4] OF INT" など）
        public int ArraySize { get; set; }
		public string ElemType { get; set; } // 配列の場合の要素型（"INT" など
        public bool IsStruct
        {
            get
            {
                return DataType == BuiltInType.ExtensionObject;
            }
        }

        public string DataTypeString { get; }

        public List<PlcVariableInfo> Children { get; set; } = new List<PlcVariableInfo>();

        private static BuiltInType ConvertType(string type)
        {
            switch(type.ToUpper())
            {
                case "BOOL": return BuiltInType.Boolean;
                case "SINT": return BuiltInType.SByte;
                case "INT": return BuiltInType.Int16;
                case "DINT": return BuiltInType.Int32;
                case "LINT": return BuiltInType.Int64;
                case "USINT": return BuiltInType.Byte;
                case "UINT": return BuiltInType.UInt16;
                case "UDINT": return BuiltInType.UInt32;
                case "ULINT": return BuiltInType.UInt64;
                case "REAL": return BuiltInType.Float;
                case "LREAL": return BuiltInType.Double;
                case "STRING": return BuiltInType.String;
                case "TIME": return BuiltInType.UInt32; // タイムは通常UInt32で表される
                case "DATE": return BuiltInType.DateTime; // 日付はDateTimeで表されることが多い
                case "TIME_OF_DAY": return BuiltInType.DateTime; // 時刻もDateTimeで表されることが多い
                case "DATE_AND_TIME": return BuiltInType.DateTime; // 日付と時刻もDateTimeで表されることが多い
                default:
                    {
                        (string elemType, _, _, _, bool isArray) = ParseArrayType(type);
                        if (isArray)
                        {
                            return ConvertType(elemType); // 配列の要素型を変換
                        }
                        else
                        {
                            return BuiltInType.ExtensionObject; // 未知の型はExtensionObjectとして扱う
                        }
                    }
            }
        }

        public static (string elemType, string subArrayType, int lowerBound, int arraySize, bool isArray) ParseArrayType(string type)
        {
            // type には ARRAY[0..9] OF INT のような形式が来ることを想定
            // ARRAY[1..3, 2..4] OF INT のような多次元配列にも対応する。
            // elemType は配列の要素型（この例では "INT"）
            // subArrayType は多次元配列の場合の次の次元の型（この例では "ARRAY[2..4] OF INT"）
            // lowerBound は配列の下限（ARRAY[1..3] OF INTの場合は 1）
            // arraySize は配列のサイズ（ARRAY[1..3] OF INTの場合は 3 - 1 + 1 = 3）
            // isArray は配列かどうか
            // 配列ではなかった場合はそのまま型を返す
            if (type.StartsWith("ARRAY[") && type.Contains("] OF "))
            {
                int startIdx = "ARRAY[".Length;
                int endIdx = type.IndexOf("] OF ");
                string boundsPart = type.Substring(startIdx, endIdx - startIdx);
                string elemType = type.Substring(endIdx + "] OF ".Length);
                var bounds = boundsPart.Split(',').Select(b => b.Trim()).ToArray();
                if (bounds.Length == 1)
                {
                    var parts = bounds[0].Split(new string[] { ".." }, StringSplitOptions.None);
                    int lowerBound = int.Parse(parts[0]);
                    int upperBound = int.Parse(parts[1]);
                    return (elemType, elemType, lowerBound, upperBound - lowerBound + 1, true);
                }
                else
                {
                    // 多次元配列の場合は、ここでは単純に最初の次元のサイズを返すことにする
                    var parts = bounds[0].Split(new string[] { ".." }, StringSplitOptions.None);
                    int lowerBound = int.Parse(parts[0]);
                    int upperBound = int.Parse(parts[1]);
                    // subArrayType は次の次元の型を返す（この例では "ARRAY[2..4] OF INT"）
                    string subArrayType = "ARRAY[" + string.Join(", ", bounds.Skip(1)) + "] OF " + elemType;
                    return (elemType, subArrayType, lowerBound, upperBound - lowerBound + 1, true);
                }
            }
            else
            {
                return (type, "", 0, 0, false); // 配列でない場合はそのまま型を返す
            }
        }


        public PlcVariableInfo(NodePublishingInfo info)
        {
            Name = info.VariableName;
            NodeId = info.VariableName;
            AccessMode = info.AccessMode;
            DataTypeString = info.DataType;
            (ElemType, SubArrayType, LowerBound, ArraySize, IsArray) = ParseArrayType(info.DataType);
        }

        public PlcVariableInfo(string name, string nodeid, string dataType, string accessMode)
        {
            Name = name;
            NodeId = nodeid;
            AccessMode = accessMode;
            DataTypeString = dataType;
            (ElemType, SubArrayType, LowerBound, ArraySize, IsArray) = ParseArrayType(dataType);
        }

        public PlcVariableInfo(string nodeid, StructFieldInfo info)
        {
            Name = info.Name;
            NodeId = nodeid;
            AccessMode = "readwrite"; // 構造体のフィールドは通常両方アクセス可能とする
            DataTypeString = info.DataType;
            (ElemType, SubArrayType, LowerBound, ArraySize, IsArray) = ParseArrayType(info.DataType);
        }
    }
}
