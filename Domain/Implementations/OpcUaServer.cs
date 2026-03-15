using Microsoft.AspNetCore.Hosting.Server;
using Opc.Ua;
using Opc.Ua.Server;
using opcua_plugin.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using MessagePack;

namespace opcua_plugin.Domain.Implementations
{
    internal class OpcUaProtocolServer : StandardServer
    {
        public NodeManager NodeManager;

        public CancellationToken CancelToken;

        public RemoteVariableStoreAccessor Accessor;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            List<INodeManager> nodeManagers = new List<INodeManager>();

            // create the custom node managers.
            NodeManager = new NodeManager(server, configuration, Accessor, CancelToken);
            nodeManagers.Add(NodeManager);

            // create master node manager.
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties();

            properties.ManufacturerName = "bamchoh";
            properties.ProductName = "Opc Ua Server";
            properties.ProductUri = "http://bamchoh.net/OpcUaServer/v1.0";
            properties.SoftwareVersion = Utils.GetAssemblySoftwareVersion();
            properties.BuildNumber = Utils.GetAssemblyBuildNumber();
            properties.BuildDate = Utils.GetAssemblyTimestamp();

            // TBD - All applications have software certificates that need to added to the properties.

            return properties;
        }

        protected override void OnServerStarted(IServerInternal server)
        {
            base.OnServerStarted(server);

            // server.SessionManager.ImpersonateUser += new ImpersonateEventHandler(SessionManager_ImpersonateUser);
        }
    }

    public static class Namespaces
    {
        public const string Empty = "http://bamchoh.net/OpcUaServer/Empty";
    }

    [DataContract(Namespace = Namespaces.Empty)]
    public class ServerConfiguration
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ServerConfiguration()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the object during deserialization.
        /// </summary>
        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
        }
        #endregion

        #region Public Properties
        #endregion

        #region Private Members
        #endregion
    }

    public class NodeManager : CustomNodeManager2, INotifyPropertyChanged
    {
        private RemoteVariableStoreAccessor _accessor;

        private Dictionary<string, PlcVariableInfo> _variables;

        // NodeId 文字列 → PlcVariableInfo のフラットマップ（配列要素・構造体フィールドを含む）
        private Dictionary<string, PlcVariableInfo> _nodeMap = new Dictionary<string, PlcVariableInfo>();

        public CancellationToken CancelToken;

        #region Private Fields
        private ServerConfiguration m_configuration;
        #endregion

        public NodeManager(IServerInternal server, ApplicationConfiguration configuration, RemoteVariableStoreAccessor accessor, CancellationToken cancellationToken)
        :
            base(server, configuration, Namespaces.Empty)
        {
            SystemContext.NodeIdFactory = this;

            // get the configuration for the node manager.
            m_configuration = configuration.ParseExtension<ServerConfiguration>();

            // use suitable defaults if no configuration exists.
            if (m_configuration == null)
            {
                m_configuration = new ServerConfiguration();
            }

            _accessor = accessor;

            CancelToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TBD
            }
        }

        public override NodeId New(ISystemContext context, NodeState node)
        {
            // generate a new numeric id in the instance namespace.
            return node.NodeId;
        }

        private void LoadFromAccessor()
        {
            // TODO: opcua-cs の文字列を上位から渡してもらうようにしたい
            var infos = _accessor.GetEnabledNodePublishings("opcua-cs");
            if (infos == null)
            {
                throw new Exception("Failed to load node tree from accessor.");
            }

            var newVars = new Dictionary<string, PlcVariableInfo>();
            foreach (var info in infos)
            {
                var v = new PlcVariableInfo(info);
                CollectAllNodeIDs(v);
                newVars[info.VariableId] = v;
            }

            _variables = newVars;

            // フラットマップを再構築
            _nodeMap.Clear();
            foreach (var v in newVars.Values)
                AddToNodeMap(v);
        }

        private void AddToNodeMap(PlcVariableInfo info)
        {
            _nodeMap[info.NodeId] = info;
            foreach (var child in info.Children)
                AddToNodeMap(child);
        }

        /*
        private NodeState CreateNodeTree(IList<IReference> references, FolderState parent = null)
        {


            var folderName = node.Name;
            FolderState folder = CreateFolder(parent, folderName, folderName);
            if (parent == null)
            {
                folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));
            }

            foreach (var child in node.Children)
            {
                if (child is Directory directory)
                {
                    CreateNodeTree(references, directory, folder);
                }
                else if (child is Variable variable)
                {
                    var vv = CreateVariable(folder, GetParentName(parent) + "/" + folderName + "/" + variable.Name, variable.Name, (uint)variable.Type);
                    DynamicVars.Add(vv);
                }
            }

            return folder;
        }
        */

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // ensure trigger can be found via the server object. 
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                LoadFromAccessor();

                var folderName = "Variables";
                FolderState folder = CreateFolder(null, folderName, folderName);
                folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));

                foreach (var _variable in _variables)
                {
                    var variable = _variable.Value;
                    Console.WriteLine($"Creating variable node: {_variable.Key} {variable.Name} ({variable.DataType})");
                    if(variable.IsStruct && !variable.IsArray)
                    {
                        folder.AddChild(CreateStructNode(folder, variable));
                    }
                    else
                    {
                        folder.AddChild(CreateVariable(folder, variable));
                    }
                }

                AddPredefinedNode(SystemContext, folder);

                // ホストの変数変更通知を受信するバックグラウンドタスクを開始
                _ = Task.Run(() => RunVariableChangeSubscriptionAsync());

                /*
                var root = CreateNodeTree(references);

                if(root == null)
                {
                    return;
                }

                // save in dictionary. 
                AddPredefinedNode(SystemContext, root);

                m_simulationTimer = new Timer(DoSimulation, null, 100, 10);
                */

                /*
                ReferenceTypeState referenceType = new ReferenceTypeState();

                referenceType.NodeId = new NodeId(3, NamespaceIndex);
                referenceType.BrowseName = new QualifiedName("IsTriggerSource", NamespaceIndex);
                referenceType.DisplayName = referenceType.BrowseName.Name;
                referenceType.InverseName = new LocalizedText("IsSourceOfTrigger");
                referenceType.SuperTypeId = ReferenceTypeIds.NonHierarchicalReferences;

                if (!externalReferences.TryGetValue(ObjectIds.Server, out references))
                {
                    externalReferences[ObjectIds.Server] = references = new List<IReference>();
                }

                trigger.AddReference(referenceType.NodeId, false, ObjectIds.Server);
                references.Add(new NodeStateReference(referenceType.NodeId, true, trigger.NodeId));

                // save in dictionary. 
                AddPredefinedNode(SystemContext, referenceType);
                */
            }
        }

        private Timer m_simulationTimer;

        public uint Value { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // quickly exclude nodes that are not in the namespace. 
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                NodeState node = null;

                if (!PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                NodeHandle handle = new NodeHandle();

                handle.NodeId = nodeId;
                handle.Node = node;
                handle.Validated = true;

                return handle;
            }
        }

        protected override NodeState ValidateNode(
            ServerSystemContext context,
            NodeHandle handle,
            IDictionary<NodeId, NodeState> cache)
        {
            // not valid if no root.
            if (handle == null)
            {
                return null;
            }

            // check if previously validated.
            if (handle.Validated)
            {
                return handle.Node;
            }

            // TBD

            return null;
        }

        private BaseObjectState CreateStructNode(NodeState parent, PlcVariableInfo info)
        {
            var objState = new Opc.Ua.BaseObjectState(parent);

            objState.NodeId = new NodeId(info.NodeId, NamespaceIndex);
            objState.BrowseName = new QualifiedName(info.NodeId, NamespaceIndex);
            objState.DisplayName = new LocalizedText("en", info.Name);
            objState.WriteMask = AttributeWriteMask.None;
            objState.UserWriteMask = AttributeWriteMask.None;

            if (info.Children.Count > 0)
            {
                foreach (var child in info.Children)
                {
                    if (child.IsStruct && !child.IsArray)
                    {
                        objState.AddChild(CreateStructNode(objState, child));
                    }
                    else
                    {
                        objState.AddChild(CreateVariable(objState, child));
                    }
                }
            }

            return objState;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, PlcVariableInfo info)
        {
            var variable = new Opc.Ua.BaseDataVariableState(parent);

            NodeId dataType = (uint)info.DataType;

            variable.SymbolicName = info.Name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.NodeId = new NodeId(info.NodeId, NamespaceIndex);
            variable.BrowseName = new QualifiedName(info.NodeId, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", info.Name);
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.DataType = dataType;

            if (info.IsArray)
            {
                variable.ValueRank = ValueRanks.OneDimension;
                variable.ArrayDimensions = new uint[] { (uint)info.ArraySize };
            }
            else
            {
                variable.ValueRank = ValueRanks.Scalar;
            }

            switch (info.AccessMode)
            {
                case "read":
                    variable.AccessLevel = AccessLevels.CurrentRead;
                    variable.UserAccessLevel = AccessLevels.CurrentRead;
                    break;
                case "write":
                    variable.AccessLevel = AccessLevels.CurrentWrite;
                    variable.UserAccessLevel = AccessLevels.CurrentWrite;
                    break;
                case "readwrite":
                default:
                    variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    break;
            }
            variable.Historizing = false;
            variable.Value = Opc.Ua.TypeInfo.GetDefaultValue(dataType, variable.ValueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            // 読み取りコールバックを設定
            variable.OnReadValue = OnReadVariable;

            if(info.Children.Count > 0)
            {
                foreach (var child in info.Children)
                {
                    if(child.IsStruct)
                    {
                        variable.AddChild(CreateStructNode(variable, child));
                    }
                    else
                    {
                        variable.AddChild(CreateVariable(variable, child));
                    }
                }
            }

            return variable;
        }

        private ServiceResult OnReadVariable(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            if (_accessor == null) return ServiceResult.Good;

            var nodeIdStr = node.NodeId.Identifier as string;
            if (nodeIdStr == null) return ServiceResult.Good;

            if (!_nodeMap.TryGetValue(nodeIdStr, out var varInfo))
            {
                statusCode = StatusCodes.BadNodeIdUnknown;
                return new ServiceResult(StatusCodes.BadNodeIdUnknown);
            }

            var (valueMessagePack, error) = _accessor.ReadVariableValue(varInfo.VariableId);
            if (!string.IsNullOrEmpty(error))
            {
                statusCode = StatusCodes.BadInternalError;
                return new ServiceResult(StatusCodes.BadInternalError);
            }

            List<ParsedNode> parsedNodes = new List<ParsedNode>();
            MessagePackPathReader.ParseNodeIdentifier(nodeIdStr, ref parsedNodes);
            int lowerBound = 0;
            if(varInfo.Parent != null)
            {
                lowerBound = varInfo.Parent.LowerBound;
            }
            MessagePackPathReader.TryGetMsgPackValue(valueMessagePack, parsedNodes, varInfo.ElemType, lowerBound, out var val);
            value = val;

            statusCode = StatusCodes.Good;
            timestamp = DateTime.UtcNow;
            return ServiceResult.Good;
        }

        public override void Write(
            OperationContext context,
            IList<WriteValue> nodesToWrite,
            IList<ServiceResult> errors)
        {
            // 基底クラスがノードの値をメモリ上に反映する
            base.Write(context, nodesToWrite, errors);

            if (_accessor == null) return;

            for (int i = 0; i < nodesToWrite.Count; i++)
            {
                if (ServiceResult.IsBad(errors[i])) continue;

                var nodeIdStr = nodesToWrite[i].NodeId.Identifier as string;
                if (nodeIdStr == null) continue;

                var rawValue = nodesToWrite[i].Value?.Value;
                var valueMsgpack = MessagePackSerializer.Serialize(rawValue);

                if (_nodeMap.TryGetValue(nodeIdStr, out var varInfo))
                {
                    var idx = -1;
                    var delimiters = new[] { '[', '.' };
                    if ((idx = nodeIdStr.IndexOfAny(delimiters)) >= 0)
                    {
                        _accessor.WriteVariableField(varInfo.VariableId, nodeIdStr.Substring(idx), valueMsgpack);
                    }
                    else
                    {
                        _accessor.WriteVariableValue(varInfo.VariableId, valueMsgpack);
                    }
                }
            }
        }

        private async Task RunVariableChangeSubscriptionAsync()
        {
            if (_accessor == null) return;
            try
            {
                using var call = _accessor.SubscribeVariableChanges();
                while (await call.ResponseStream.MoveNext(CancelToken))
                {
                    var change = call.ResponseStream.Current;
                    if (!_nodeMap.TryGetValue(change.VariableId, out var varInfo)) continue;

                    var nodeId = new NodeId(change.VariableId, NamespaceIndex);
                    lock (Lock)
                    {
                        if (!PredefinedNodes.TryGetValue(nodeId, out var nodeState)) continue;
                        if (nodeState is not BaseDataVariableState variable) continue;

                        variable.Value = JsonToOpcUaValue(change.ValueMsgpack.ToByteArray(), varInfo.DataType, varInfo.IsArray, varInfo.ArraySize);
                        variable.StatusCode = StatusCodes.Good;
                        variable.Timestamp = DateTime.UtcNow;
                    }
                    // ロック外で変更マスクをクリアして購読クライアントに通知
                    PredefinedNodes[nodeId].ClearChangeMasks(SystemContext, false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"SubscribeVariableChanges error: {ex.Message}");
            }
        }

        private static object JsonToOpcUaValue(byte[] bytes, BuiltInType dataType, bool isArray, int arraySize)
        {
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                using var doc = JsonDocument.Parse("");
                var root = doc.RootElement;
                if (isArray && root.ValueKind == JsonValueKind.Array)
                {
                    var items = root.EnumerateArray().ToArray();
                    return ConvertJsonArray(items, dataType);
                }
                return ConvertJsonElement(root, dataType);
            }
            catch
            {
                return null;
            }
        }

        private static object ConvertJsonArray(JsonElement[] items, BuiltInType dataType)
        {
            return dataType switch
            {
                BuiltInType.Boolean => items.Select(e => e.GetBoolean()).ToArray(),
                BuiltInType.SByte   => items.Select(e => e.GetSByte()).ToArray(),
                BuiltInType.Byte    => items.Select(e => e.GetByte()).ToArray(),
                BuiltInType.Int16   => items.Select(e => e.GetInt16()).ToArray(),
                BuiltInType.UInt16  => items.Select(e => e.GetUInt16()).ToArray(),
                BuiltInType.Int32   => items.Select(e => e.GetInt32()).ToArray(),
                BuiltInType.UInt32  => items.Select(e => e.GetUInt32()).ToArray(),
                BuiltInType.Int64   => items.Select(e => e.GetInt64()).ToArray(),
                BuiltInType.UInt64  => items.Select(e => e.GetUInt64()).ToArray(),
                BuiltInType.Float   => items.Select(e => e.GetSingle()).ToArray(),
                BuiltInType.Double  => items.Select(e => e.GetDouble()).ToArray(),
                BuiltInType.String  => items.Select(e => e.GetString()).ToArray(),
                _                   => null,
            };
        }

        private static object ConvertJsonElement(JsonElement element, BuiltInType dataType)
        {
            return dataType switch
            {
                BuiltInType.Boolean => element.GetBoolean(),
                BuiltInType.SByte   => element.GetSByte(),
                BuiltInType.Byte    => element.GetByte(),
                BuiltInType.Int16   => element.GetInt16(),
                BuiltInType.UInt16  => element.GetUInt16(),
                BuiltInType.Int32   => element.GetInt32(),
                BuiltInType.UInt32  => element.GetUInt32(),
                BuiltInType.Int64   => element.GetInt64(),
                BuiltInType.UInt64  => element.GetUInt64(),
                BuiltInType.Float   => element.GetSingle(),
                BuiltInType.Double  => element.GetDouble(),
                BuiltInType.String  => element.GetString(),
                _                   => null,
            };
        }

        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            var folder = new FolderState(parent);

            folder.SymbolicName = name;
            folder.ReferenceTypeId = ReferenceTypes.Organizes;
            folder.TypeDefinitionId = ObjectTypeIds.FolderType;
            folder.NodeId = new NodeId(path, NamespaceIndex);
            folder.BrowseName = new QualifiedName(path, NamespaceIndex);
            folder.DisplayName = new LocalizedText("en", name);
            folder.WriteMask = AttributeWriteMask.None;
            folder.UserWriteMask = AttributeWriteMask.None;
            folder.EventNotifier = EventNotifiers.None;

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }

        /*
        protected override void OnMonitoredItemCreated(ServerSystemContext context, NodeHandle handle, MonitoredItem monitoredItem)
        {
            Console.WriteLine(MonitoredItems.Count);
            foreach (var v in m_dynamicVars)
            {
                v.Value = (UInt32)v.Value + 1;
                v.StatusCode = StatusCodes.Good;
                v.Timestamp = DateTime.UtcNow;
                v.ClearChangeMasks(SystemContext, false);
            }

            if (MonitoredItems.Count >= 2)
            {
                foreach (var v in m_dynamicVars)
                {
                    v.StatusCode = StatusCodes.BadUnexpectedError;
                    v.Timestamp = DateTime.UtcNow;
                    v.ClearChangeMasks(SystemContext, false);
                }
            }
        }

        protected override void OnMonitoredItemDeleted(ServerSystemContext context, NodeHandle handle, MonitoredItem monitoredItem)
        {
            Console.WriteLine(MonitoredItems.Count);
            foreach (var v in m_dynamicVars)
            {
                v.Value = (UInt32)v.Value + 1;
                v.StatusCode = StatusCodes.Good;
                v.Timestamp = DateTime.UtcNow;
                v.ClearChangeMasks(SystemContext, false);
            }

            if (MonitoredItems.Count >= 2)
            {
                foreach (var v in m_dynamicVars)
                {
                    v.StatusCode = StatusCodes.BadNotReadable;
                    v.Timestamp = DateTime.UtcNow;
                    v.ClearChangeMasks(SystemContext, false);
                }
            }
        }
        */

        /*
        // collectAllNodeIDs は指定ノード（nodeStr）とその子ノード全ての NodeID 文字列を返す
        func (ns *PLCNameSpace) collectAllNodeIDs(varID, dataType, nodeStr string) []string {
            result := []string{nodeStr}
            elemType, size, isArr := parseArrayType(dataType)
            if isArr {
                for i := 0; i < int(size); i++ {
                    child := fmt.Sprintf("%s[%d]", nodeStr, i)
                    result = append(result, ns.collectAllNodeIDs(varID, elemType, child)...)
                }
                return result
            }
            if isStructDataType(dataType) && ns.accessor != nil {
                for _, f := range ns.accessor.GetStructFields(dataType) {
                    child := nodeStr + "." + f.Name
                    result = append(result, ns.collectAllNodeIDs(varID, f.DataType, child)...)
                }
            }
            return result
        }
        */

        private void CollectAllNodeIDs(PlcVariableInfo info, string parentNodePath = "")
        {
            var result = new List<PlcVariableInfo>() {};
            if (info.IsArray)
            {
                for (int i = 0; i < info.ArraySize; i++)
                {
                    var childName = $"[{i+info.LowerBound}]";
                    var nodeId = $"{info.NodeId}{childName}";
                    var childInfo = new PlcVariableInfo(childName, nodeId, info);
                    CollectAllNodeIDs(childInfo, "");
                    info.Children.Add(childInfo);
                }
                return;
            }

            if (info.IsStruct && _accessor != null)
            {
                var fields = _accessor.GetStructFields(info.DataTypeString);
                foreach (StructFieldInfo f in fields)
                {
                    var child = $"{info.NodeId}.{f.Name}";
                    var childInfo = new PlcVariableInfo(child, f, info);
                    CollectAllNodeIDs(childInfo, child);
                    info.Children.Add(childInfo);
                }
            }
        }
    }
}
