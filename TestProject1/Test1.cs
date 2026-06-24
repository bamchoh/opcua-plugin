using Google.Protobuf;
using MessagePack;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Opc.Ua;
using Opc.Ua.Server;
using opcua_plugin.Domain.Implementations;
using opcua_plugin.Infrastructure;
using System.Reflection.Metadata;

namespace TestProject1
{
    [TestClass]
    public sealed class Test1
    {
        [DataTestMethod]
        [DataRow("ULINT", BuiltInType.UInt64, false, false, 0, 0, "")]
        [DataRow("INT", BuiltInType.Int16, false, false, 0, 0, "")]
        [DataRow("DINT", BuiltInType.Int32, false, false, 0, 0, "")]
        [DataRow("REAL", BuiltInType.Float, false, false, 0, 0, "")]
        [DataRow("ARRAY[1..2] OF INT", BuiltInType.Int16, true, false, 1, 2, "INT")]
        [DataRow("ARRAY[1..2, 2..5] OF INT", BuiltInType.Int16, true, false, 1, 2, "ARRAY[2..5] OF INT")]
        public void TestMethod1(string typeString, BuiltInType expectedType, bool isArray, bool isStruct, int lowerBound, int arraySize, string subArrayType)
        {
            var node = new NodePublishingInfo()
            {
                VariableName = "Test",
                DataType = typeString,
                VariableId = "1234-5678-9012",
                AccessMode = "readwrite"
            };
            var info = new PlcVariableInfo(node);
            Assert.AreEqual(info.DataType, expectedType);
            Assert.AreEqual(info.IsArray, isArray);
            Assert.AreEqual(info.LowerBound, lowerBound);
            Assert.AreEqual(info.ArraySize, arraySize);
            Assert.AreEqual(info.SubArrayType, subArrayType);
            Assert.AreEqual(info.IsStruct, isStruct);
        }


        public static IEnumerable<object[]> GetTestData()
        {
            yield return new object[] { "Primitive", "INT", (Int16)12345, MessagePackSerializer.Serialize((Int16)12345)};

            yield return new object[] { "Array1D[3]", "INT", (Int16)12345, MessagePackSerializer.Serialize(
                new Int16[] {
                    1, 2, 3, 12345, 4
                } ) };

            yield return new object[] { "Array1D[3][4]", "INT", (Int16)12345, MessagePackSerializer.Serialize(
                new Int16[][] {
                    new Int16[] { 1, 2, 3, 4, 5 },
                    new Int16[] { 6, 7, 8, 9, 10 },
                    new Int16[] { 11, 12, 13, 14, 15 },
                    new Int16[] { 16, 17, 18, 19, 12345 },
                } ) };

            var msgpack1 = MessagePackSerializer.Serialize(
                new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "Field1", 12345 },
                        { "Field2", "value1" },
                        { "Field3", true }
                    },
                    new Dictionary<string, object>
                    {
                        { "Field1", 23456 },
                        { "Field2", "value2" },
                        { "Field3", false }
                    }
                });

            yield return new object[] { "Struct1[0].Field1", "INT", (Int16)12345, msgpack1 };
            yield return new object[] { "Struct1[0].Field2", "STRING", "value1", msgpack1 };
            yield return new object[] { "Struct1[0].Field3", "BOOL", true, msgpack1 };
            yield return new object[] { "Struct1[1].Field1", "INT", (Int16)23456, msgpack1 };
            yield return new object[] { "Struct1[1].Field2", "STRING", "value2", msgpack1 };
            yield return new object[] { "Struct1[1].Field3", "BOOL", false, msgpack1 };

            var msgpack3 = MessagePackSerializer.Serialize(
                new Dictionary<string, object>
                {
                    { "Field1", (UInt32)12345678 },
                    { "Field2", "value1" },
                    { "Field3", true }
                });

            yield return new object[] { "StructTest1.Field1", "DINT", 12345678, msgpack3 };
            yield return new object[] { "StructTest1.Field2", "STRING", "value1", msgpack3 };
            yield return new object[] { "StructTest1.Field3", "BOOL", true, msgpack3 };

            yield return new object[] { "Array1D", "ARRAY[0..4] OF INT", (Int16)12345, MessagePackSerializer.Serialize(
                new Int16[] {
                    1, 2, 3, 12345, 4
                } ) };
        }


        [DataTestMethod]
        [DynamicData(nameof(GetTestData), DynamicDataSourceType.Method)]
        public void GetValueFromMsgPack_ByIdentifierPath_ReturnsExpectedValue(string identifier, string datatype, object expected, byte[] msgpackbin)
        {
            var result = new List<ParsedNode>();
            var success = MessagePackPathReader.ParseNodeIdentifier(identifier, ref result);

            object? val;
            MessagePackPathReader.TryGetMsgPackValue(msgpackbin, result, datatype, 0, out val);

            Assert.AreEqual(expected, val);
        }

        public static IEnumerable<object[]> ParseNodeIdentifierTestData()
        {
            yield return new object[] { "Primitive", new List<ParsedNode>() {
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Primitive, Index = -1 }
            } };

            yield return new object[] { "Struct1.Field1", new List<ParsedNode>() {
                new ParsedNode { Key = "Field1", Type = ParsedNode.NodeType.Map, Index = -1 },
            } };

            yield return new object[] { "Struct1.Array1[10]", new List<ParsedNode>() {
                new ParsedNode { Key = "Array1", Type = ParsedNode.NodeType.Map, Index = -1 },
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 10 }
            } };

            yield return new object[] { "Struct1.SubStruct1.Field1", new List<ParsedNode>() {
                new ParsedNode { Key = "SubStruct1", Type = ParsedNode.NodeType.Map, Index = -1 },
                new ParsedNode { Key = "Field1", Type = ParsedNode.NodeType.Map, Index = -1 }
            } };

            yield return new object[] { "Array1D[3]", new List<ParsedNode>() {
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 3 }
            } };

            yield return new object[] { "Array2D[3][4]", new List<ParsedNode>() {
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 3 },
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 4 },
            } };

            yield return new object[] { "Array3D[3][4][5]", new List<ParsedNode>() {
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 3 },
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 4 },
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 5 }
            } };

            yield return new object[] { "Struct1[0].Field1", new List<ParsedNode>() {
                new ParsedNode { Key = "", Type = ParsedNode.NodeType.Array, Index = 0 },
                new ParsedNode { Key = "Field1", Type = ParsedNode.NodeType.Map }
            } };
        }


        [DataTestMethod]
        [DynamicData(nameof(ParseNodeIdentifierTestData), DynamicDataSourceType.Method)]
        public void ParseNodeIdentifierTest(string identifier, List<ParsedNode>nodelist)
        {
            var result = new List<ParsedNode>();
            MessagePackPathReader.ParseNodeIdentifier(identifier, ref result);
            Assert.AreEqual(nodelist.Count, result.Count);
            for(int i = 0; i < nodelist.Count; i++)
            {
                Assert.AreEqual(nodelist[i].Type, result[i].Type);
                Assert.AreEqual(nodelist[i].Index, result[i].Index);
                Assert.AreEqual(nodelist[i].Key, result[i].Key);
            }
        }
    }

}