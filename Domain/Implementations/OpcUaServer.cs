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
                var v = new PlcVariableInfo(info);
                CollectAllNodeIDs(v);
                newVars[info.VariableId] = v;
            }

            _variables = newVars;
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
                    folder.AddChild(CreateVariable(folder, variable));
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

            if(info.IsArray)
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

            if(info.Children.Count > 0)
            {
                foreach (var child in info.Children)
                {
                    variable.AddChild(CreateVariable(variable, child));
                }
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
                    var childInfo = new PlcVariableInfo(childName, nodeId, info.SubArrayType, info.AccessMode);
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
                    var childInfo = new PlcVariableInfo(child, f);
                    CollectAllNodeIDs(childInfo, child);
                    info.Children.Add(childInfo);
                }
            }
        }
    }
}
