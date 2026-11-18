using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RiRtoPY
{
    [DataContract] public sealed class Graph
    {
        [DataMember] public string Schema = "grasshopper-analysis/3";
        [DataMember] public string Version = "0.5.0";
        [DataMember] public string Document;
        [DataMember] public string Host;
        [DataMember] public string FilePath;
        [DataMember] public string Scope;
        [DataMember] public bool Complete = true;
        [DataMember] public List<string> Issues = new List<string>();
        [DataMember] public List<GroupRecord> Groups = new List<GroupRecord>();
        [DataMember] public List<Node> Nodes = new List<Node>();
    }
    [DataContract] public sealed class Node
    {
        [DataMember] public string Id;
        [DataMember] public string InstanceGuid;
        [DataMember] public Graph Cluster;
        [DataMember] public bool IsCluster;
        [DataMember] public List<BoundaryLink> BoundaryLinks = new List<BoundaryLink>();
        [DataMember] public List<string> GroupIds = new List<string>();
        [DataMember] public bool Locked;
        [DataMember] public List<string> Settings = new List<string>();
        [DataMember] public List<Port> OutputPorts = new List<Port>();
        [DataMember] public string ComponentGuid;
        [DataMember] public string Name;
        [DataMember] public string NickName;
        [DataMember] public string Type;
        [DataMember] public string Assembly;
        [DataMember] public string Description;
        [DataMember] public string SourceText;
        [DataMember] public List<string> Groups = new List<string>();
        [DataMember] public bool Selected;
        [DataMember] public string Operation;
        [DataMember] public string Literal;
        [DataMember] public List<Port> Inputs = new List<Port>();
        [DataMember] public List<string> Outputs = new List<string>();
        [DataMember] public List<string> Issues = new List<string>();
    }
    [DataContract] public sealed class Port
    {
        [DataMember] public string Id;
        [DataMember] public string NickName;
        [DataMember] public string Type;
        [DataMember] public string Description;
        [DataMember] public DataSnapshot Persistent;
        [DataMember] public DataSnapshot Volatile;
        [DataMember] public bool Optional;
        [DataMember] public bool HasPersistentData;
        [DataMember] public string Name;
        [DataMember] public string Access;
        [DataMember] public string Mapping;
        [DataMember] public bool Reverse;
        [DataMember] public bool Simplify;
        [DataMember] public string Expression;
        [DataMember] public string Literal;
        [DataMember] public List<Wire> Sources = new List<Wire>();
    }
    [DataContract] public sealed class DataSnapshot
    {
        [DataMember] public string Status;
        [DataMember] public string Error;
        [DataMember] public int ItemCount;
        [DataMember] public List<DataBranch> Branches = new List<DataBranch>();
    }
    [DataContract] public sealed class DataBranch
    {
        [DataMember] public string Path;
        [DataMember] public List<DataValue> Values = new List<DataValue>();
    }
    [DataContract] public sealed class DataValue
    {
        [DataMember] public string Type;
        [DataMember] public string Text;
        [DataMember] public bool IsNull;
    }
    [DataContract] public sealed class GroupRecord
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public List<string> Members = new List<string>();
    }
    [DataContract] public sealed class BoundaryLink
    {
        [DataMember] public string Direction;
        [DataMember] public int ExternalPort;
        [DataMember] public string InternalNode;
    }
    [DataContract] public sealed class Wire
    {
        [DataMember] public string Node;
        [DataMember] public int Output;
    }
}
