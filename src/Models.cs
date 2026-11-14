using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RiRtoPY
{
    [DataContract] public sealed class Graph
    {
        [DataMember] public string Schema = "rir-to-python/2";
        [DataMember] public string Version = "0.3.0";
        [DataMember] public string Document;
        [DataMember] public string Host;
        [DataMember] public List<Node> Nodes = new List<Node>();
    }
    [DataContract] public sealed class Node
    {
        [DataMember] public string Id;
        [DataMember] public string ComponentGuid;
        [DataMember] public string Name;
        [DataMember] public string NickName;
        [DataMember] public string Type;
        [DataMember] public string Assembly;
        [DataMember] public bool Selected;
        [DataMember] public string Operation;
        [DataMember] public string Literal;
        [DataMember] public List<Port> Inputs = new List<Port>();
        [DataMember] public List<string> Outputs = new List<string>();
        [DataMember] public List<string> Issues = new List<string>();
    }
    [DataContract] public sealed class Port
    {
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
    [DataContract] public sealed class Wire
    {
        [DataMember] public string Node;
        [DataMember] public int Output;
    }
}
