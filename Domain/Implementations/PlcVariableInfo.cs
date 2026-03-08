using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcua_plugin.Domain.Implementations
{
    public class PlcVariableInfo
    {
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
		public int ArraySize { get; set; }
		public string ElemType { get; set; } // 配列の場合の要素型（"INT" など

        public string DataTypeString { get; set; }

        private static BuiltInType ConvertType(string type)
        {
            return type.ToUpper() switch
            {
                "BOOL" => BuiltInType.Boolean,
                "SINT" => BuiltInType.SByte,
                "INT" => BuiltInType.Int16,
                "DINT" => BuiltInType.Int32,
                "LINT" => BuiltInType.Int64,
                "USINT" => BuiltInType.Byte,
                "UINT" => BuiltInType.UInt16,
                "UDINT" => BuiltInType.UInt32,
                "ULINT" => BuiltInType.UInt64,
                "REAL" => BuiltInType.Float,
                "LREAL" => BuiltInType.Double,
                "STRING" => BuiltInType.String,
                "TIME" => BuiltInType.UInt32,
                "DATE" => BuiltInType.DateTime,
                "TIME_OF_DAY" => BuiltInType.DateTime,
                "DATE_AND_TIME" => BuiltInType.DateTime,
                _ => throw new Exception($"Unknown type {type}")
            };
        }
    }
}
