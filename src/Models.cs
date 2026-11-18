using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RiRtoPY
{
    [DataContract] public sealed class Graph
    {
        [DataMember] public string Schema = "grasshopper-analysis/4";
        [DataMember] public string Version = "0.6.0";
        [DataMember] public string Document;
        [DataMember] public string Host;
        [DataMember] public string FilePath;
        [DataMember] public string Scope;
        [DataMember] public bool IsPartial;
        [DataMember] public CaptureState Capture = new CaptureState();
        [DataMember] public List<Diagnostic> Diagnostics = new List<Diagnostic>();
        [DataMember] public List<string> ExportNotes = new List<string>();
        [DataMember] public bool Complete = true;
        [DataMember] public List<string> Issues = new List<string>();
        [DataMember] public List<GroupRecord> Groups = new List<GroupRecord>();
        [DataMember] public List<Node> Nodes = new List<Node>();
    }
    [DataContract] public sealed class Node
    {
        [DataMember] public string ExportAlias;
        [DataMember] public string Id;
        [DataMember] public string InstanceGuid;
        [DataMember] public Graph Cluster;
        [DataMember] public bool IsCluster;
        [DataMember] public List<BoundaryLink> BoundaryLinks = new List<BoundaryLink>();
        [DataMember] public List<string> GroupIds = new List<string>();
        [DataMember] public bool Locked;
        [DataMember] public CanvasInfo Canvas;
        [DataMember] public string Annotation;
        [DataMember] public string AnnotationFont;
        [DataMember] public List<double> AnnotationCorners = new List<double>();
        [DataMember] public ScriptSnapshot Script;
        [DataMember] public List<string> RuntimeErrors = new List<string>();
        [DataMember] public List<string> RuntimeWarnings = new List<string>();
        [DataMember] public string RuntimeStatus;
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
        [DataMember] public string TypeHint;
        [DataMember] public bool Locked;
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
        [DataMember] public GeometrySummary Geometry;
    }
    [DataContract] public sealed class GroupRecord
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public string RawName;
        [DataMember] public string Description;
        [DataMember] public string Colour;
        [DataMember] public string Border;
        [DataMember] public CanvasInfo Canvas;
        [DataMember] public List<string> Members = new List<string>();
    }
    [DataContract] public sealed class CaptureState
    {
        [DataMember] public string DocumentId;
        [DataMember] public string CapturedAtUtc;
        [DataMember] public string FinishedAtUtc;
        [DataMember] public string RhinoVersion;
        [DataMember] public string GrasshopperVersion;
        [DataMember] public bool? Modified;
        [DataMember] public bool SavedFile;
        [DataMember] public string SolutionState;
        [DataMember] public string LastSolutionUtc;
        [DataMember] public string ResultValidity = "unknown; solver not run by exporter";
        [DataMember] public string ModelUnits;
    }
    [DataContract] public sealed class CanvasInfo
    {
        [DataMember] public string Reason;
        [DataMember] public double X;
        [DataMember] public double Y;
        [DataMember] public double Left;
        [DataMember] public double Top;
        [DataMember] public double Width;
        [DataMember] public double Height;
        [DataMember] public string Status;
    }
    [DataContract] public sealed class ScriptSnapshot
    {
        [DataMember] public string Status;
        [DataMember] public string Reason;
        [DataMember] public string Language;
        [DataMember] public string Adapter;
        [DataMember] public string ComponentType;
        [DataMember] public string EngineVersion;
        [DataMember] public List<ScriptPart> Parts = new List<ScriptPart>();
        [DataMember] public List<string> References = new List<string>();
    }
    [DataContract] public sealed class ScriptPart
    {
        [DataMember] public string Name;
        [DataMember] public string Text;
    }
    [DataContract] public sealed class Diagnostic
    {
        [DataMember] public string Category;
        [DataMember] public string Severity;
        [DataMember] public string Node;
        [DataMember] public string Message;
    }
    [DataContract] public sealed class GeometrySummary
    {
        [DataMember] public string Status;
        [DataMember] public string Type;
        [DataMember] public string Reason;
        [DataMember] public List<string> Properties = new List<string>();
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
