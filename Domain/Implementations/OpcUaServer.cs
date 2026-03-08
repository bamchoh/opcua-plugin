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
using System.Threading.Tasks;
using System.Xml.Linq;

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

        private List<string> _allNodeIDs;

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

        private string GetParentName(NodeState node)
        {
            if (node == null)
                return "";

            return GetParentName((node as FolderState)?.Parent) + "/" + node.DisplayName;
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
                var v = new PlcVariableInfo()
                {
                    Name = info.VariableName,
                    AccessMode = info.AccessMode,
                    DataTypeString = info.DataType,
                };
                (v.ElemType, v.ArraySize, v.IsArray) = ParseArrayType(info.DataType);
                newVars[info.VariableId] = v;
            }

            var allIDs = new List<string>();
            foreach (var varId in newVars.Keys)
            {
                var nodeIDs = CollectAllNodeIDs(varId, newVars[varId].DataTypeString, varId);
                foreach(var nodeID in nodeIDs)
                {
                    allIDs.Add(nodeID);
                }
            }

            _variables = newVars;
            _allNodeIDs = allIDs;
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
                    Console.WriteLine($"Creating variable node: {variable.Name} ({variable.DataType})");
                    CreateVariable(folder, variable.Name, variable.Name, (uint)variable.DataType);
                }

                AddPredefinedNode(SystemContext, folder);

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

        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType)
        {
            var valueRank = ValueRanks.Scalar;

            var variable = new Opc.Ua.BaseDataVariableState(parent);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = Opc.Ua.TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
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

        private (string elemType, int size, bool isArray) ParseArrayType(string dataType)
        {
            if (!dataType.StartsWith("ARRAY[") || !dataType.EndsWith("]"))
            {
                return ("", 0, false);
            }
            var inner = dataType.Substring("ARRAY[".Length, dataType.Length - "ARRAY[".Length - 1);
            var idx = inner.LastIndexOf(';');
            if (idx < 0)
            {
                return ("", 0, false);
            }
            var et = inner.Substring(0, idx).Trim();
            var sizeStr = inner.Substring(idx + 1).Trim();
            if (!int.TryParse(sizeStr, out var n) || n <= 0)
            {
                return ("", 0, false);
            }
            return (et, n, true);
        }

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

        private List<string> CollectAllNodeIDs(string varID, string dataType, string nodeStr)
        {
            var result = new List<string>() { nodeStr };
            (var elemType, var size, var isArr) = ParseArrayType(dataType);
            if (isArr)
            {
                for (int i = 0; i < size; i++)
                {
                    var child = $"{nodeStr}[{i}]";
                    result.Add(child);
                }
                return result;
            }

            if (IsStructDataType(dataType) && _accessor != null)
            {
                var fields = _accessor.GetStructFields(dataType);
                foreach (var f in fields)
                {
                    var child = $"{nodeStr}.{f.Name}";
                    result.AddRange(CollectAllNodeIDs(varID, f.DataType, child));
                }
            }
            return result;
        }

        public bool IsStructDataType(string dataType)
        {
            if (string.IsNullOrEmpty(dataType))
            {
                return false;
            }
            var (_, _, isArr) = ParseArrayType(dataType);
            if (isArr)
            {
                return false;
            }
            switch (dataType)
            {
                case "BOOL":
                case "SINT":
                case "INT":
                case "DINT":
                case "LINT":
                case "USINT":
                case "UINT":
                case "UDINT":
                case "ULINT":
                case "REAL":
                case "LREAL":
                case "STRING":
                case "TIME":
                case "DATE":
                case "TIME_OF_DAY":
                case "DATE_AND_TIME":
                    return false;
            }
            if (dataType.StartsWith("STRING[") && dataType.EndsWith("]"))
            {
                return false;
            }
            return true;
        }
    }
}
