using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using GH_IO.Serialization;

namespace RiRtoPY
{
    // Read-only snapshot. Never collects data, solves, expires or modifies the document.
    public static class GraphReader
    {
        private sealed class SolutionStamp { public string LastUtc; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GH_Document,SolutionStamp> Stamps = new System.Runtime.CompilerServices.ConditionalWeakTable<GH_Document,SolutionStamp>();
        private static SolutionStamp Observe(GH_Document doc)
        {return Stamps.GetValue(doc,d=>{var stamp=new SolutionStamp();d.SolutionEnd+=(sender,args)=>{stamp.LastUtc=DateTime.UtcNow.ToString("o");};return stamp;});}
        private static object Property(object value, string name)
        {
            if (value == null) return null;
            var p = value.GetType().GetProperty(name);
            return p == null ? null : p.GetValue(value, null);
        }
        private static string Error(Exception ex) { return (ex.InnerException ?? ex).Message; }
        private static CanvasInfo Canvas(IGH_DocumentObject obj)
        {
            if(obj.Attributes==null)return new CanvasInfo { Status="unavailable",Reason="Attributs visuels absents." };
            var a=obj.Attributes;var box=a.Bounds;
            return new CanvasInfo { Status=box.IsEmpty?"partial":"captured",Reason=box.IsEmpty?"Limites visuelles non calculees ; pivot seulement.":null,X=a.Pivot.X,Y=a.Pivot.Y,Left=box.Left,Top=box.Top,Width=box.Width,Height=box.Height };
        }
        private static string GroupName(GH_Group group)
        { return String.IsNullOrWhiteSpace(group.NickName)?"Groupe sans nom ["+group.InstanceGuid.ToString().Substring(0,8)+"]":group.NickName; }
        private static Dictionary<string,string> ScriptArchive(IGH_DocumentObject obj)
        {
            var chunk=new GH_LooseChunk("Script");
            if(!obj.Write(chunk))throw new InvalidOperationException("Serialisation du composant script refusee.");
            var result=new Dictionary<string,string>();
            foreach(var key in new[]{"UsingSource","ScriptSource","AdditionalSource"})if(chunk.ItemExists(key))result[key]=chunk.GetString(key);
            int count=chunk.ItemExists("ReferenceCount")?chunk.GetInt32("ReferenceCount"):0;
            for(int i=0;i<count;i++)result["Reference["+i+"]"]=chunk.GetString("Reference",i);
            return result;
        }
        private static IGH_DocumentObject Owner(IGH_Param p)
        { return p.Attributes == null ? p : p.Attributes.GetTopLevel.DocObject; }

        private static DataSnapshot Capture(Func<IGH_Structure> read)
        {
            var result = new DataSnapshot();
            try
            {
                var tree = read();
                if (tree == null) { result.Status = "unavailable"; return result; }
                result.Status = tree.DataCount == 0 ? "empty" : "captured";
                result.ItemCount = tree.DataCount;
                for (int i = 0; i < tree.PathCount; i++)
                {
                    var branch = new DataBranch { Path = tree.get_Path(i).ToString() };
                    result.Branches.Add(branch);
                    foreach (var value in tree.get_Branch(i))
                    {
                        var item = new DataValue { IsNull = value == null, Type = value == null ? null : value.GetType().FullName };
                        branch.Values.Add(item);
                        try { item.Text = value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture); }
                        catch (Exception ex) { result.Status = "partial"; result.Error = Error(ex); }
                        if(value!=null)item.Geometry=GeometryCapture.Read(value);
                    }
                }
            }
            catch (Exception ex) { result.Status = "error"; result.Error = Error(ex); }
            return result;
        }
        private static Port ReadPort(IGH_Param p, string scope, bool sources)
        {
            var result = new Port {
                Id = scope + "/" + p.InstanceGuid, Name = p.Name, NickName = p.NickName,
                Description = p.Description, Type = p.GetType().FullName,
                TypeHint=Convert.ToString(Property(p,"TypeHint"),CultureInfo.InvariantCulture),Locked=p.Locked,
                Optional = p.Optional, Access = p.Access.ToString().ToLowerInvariant(),
                Mapping = p.DataMapping.ToString(), Reverse = p.Reverse, Simplify = p.Simplify,
                Expression = Convert.ToString(Property(p, "Expression"), CultureInfo.InvariantCulture),
                Persistent = Capture(() => Property(p, "PersistentData") as IGH_Structure),
                Volatile = Capture(() => p.VolatileData)
            };
            result.HasPersistentData = result.Persistent.ItemCount > 0;
            if (sources) foreach (var source in p.Sources)
            {
                var owner = Owner(source);
                var component = owner as IGH_Component;
                result.Sources.Add(new Wire { Node = scope + "/" + owner.InstanceGuid, Output = component == null ? 0 : component.Params.Output.IndexOf(source) });
            }
            return result;
        }
        private static void Settings(IGH_DocumentObject o, Node n)
        {
            foreach (string name in new[] { "CurrentValue", "Value" })
            {
                var value = Property(o, name);
                if (value != null) n.Settings.Add(name + " = " + Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            var slider = Property(o, "Slider");
            if (slider != null) foreach (string name in new[] { "Minimum", "Maximum", "DecimalPlaces", "Type" })
                n.Settings.Add("Slider." + name + " = " + Convert.ToString(Property(slider, name), CultureInfo.InvariantCulture));
            var items = Property(o, "ListItems") as IEnumerable;
            if (items != null) foreach (var item in items)
                n.Settings.Add("Choix : " + Property(item, "Name") + " | expression=" + Property(item, "Expression") + " | selection=" + Property(item, "Selected"));
        }
        private static void Boundaries(GH_Cluster cluster, Node node, string internalScope)
        {
            // The serialized ParamMap is the actual GUID mapping. Never infer it from
            // hook position, caption or document enumeration order (all can differ).
            var chunk = new GH_LooseChunk("Cluster");
            if (!cluster.Write(chunk)) throw new InvalidOperationException("Lecture de la correspondance du cluster impossible.");
            var map = chunk.FindChunk("ParamMap");
            if (map == null) throw new InvalidOperationException("Correspondance ParamMap absente.");
            var lookup = new Dictionary<Guid, Guid>();
            for (int i = 0; i < map.GetInt32("Count"); i++) lookup[map.GetGuid("Key", i)] = map.GetGuid("Value", i);
            foreach (var side in new[] { cluster.Params.Input, cluster.Params.Output })
            {
                bool input = Object.ReferenceEquals(side, cluster.Params.Input);
                for (int i = 0; i < side.Count; i++)
                {
                    Guid hook;
                    if (!lookup.TryGetValue(side[i].InstanceGuid, out hook)) throw new InvalidOperationException("Port de cluster sans correspondance : " + side[i].Name);
                    string id = internalScope + "/" + hook;
                    string hookType = input ? typeof(GH_ClusterInputHook).FullName : typeof(GH_ClusterOutputHook).FullName;
                    if (!node.Cluster.Nodes.Any(n => n.Id == id && n.Type == hookType)) throw new InvalidOperationException("Point de passage interne absent ou incoherent : " + hook);
                    node.BoundaryLinks.Add(new BoundaryLink { Direction = input ? "input" : "output", ExternalPort = i, InternalNode = id });
                }
            }
        }
        public static Graph Read(GH_Document doc, bool selectionOnly)
        {
            if (selectionOnly) throw new InvalidOperationException("L'export complet porte sur tout le document, sans selection partielle.");
            if (doc == null) throw new InvalidOperationException("Ouvrir un document Grasshopper.");
            var result=ReadDocument(doc, "main", new HashSet<GH_Document>());
            GraphAnalysis.Audit(result);
            return result;
        }
        private static Graph ReadDocument(GH_Document doc, string scope, HashSet<GH_Document> stack)
        {
            var graph = new Graph { Document = doc.DisplayName, FilePath = doc.FilePath, Scope = scope, Host = Rhino.RhinoApp.Version.ToString() };
            graph.Capture=new CaptureState { CapturedAtUtc=DateTime.UtcNow.ToString("o"),RhinoVersion=graph.Host,
                GrasshopperVersion=typeof(GH_Document).Assembly.GetName().Version.ToString(),Modified=doc.IsModified,
                SavedFile=doc.IsFilePathDefined,SolutionState=doc.SolutionState.ToString(),
                LastSolutionUtc=Observe(doc).LastUtc,DocumentId=doc.DocumentID.ToString(),
                ModelUnits=Rhino.RhinoDoc.ActiveDoc==null?"unknown":Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString() };
            if (!stack.Add(doc)) { graph.Complete = false; graph.Issues.Add("Reference de document cyclique : contenu deja en cours de lecture."); return graph; }
            try
            {
                var groups = doc.Objects.OfType<GH_Group>().ToList();
                foreach (var group in groups)
                    graph.Groups.Add(new GroupRecord { Id = scope + "/" + group.InstanceGuid, Name = GroupName(group),RawName=group.NickName,
                        Description=group.Description,Colour=group.Colour.ToArgb().ToString("X8"),Border=group.Border.ToString(),Canvas=Canvas(group),Members = group.ObjectIDs.Select(id => scope + "/" + id).ToList() });
                foreach (var o in doc.Objects.Where(o => !(o is RiRComponent) && !(o is GH_Group)))
                {
                    var n = new Node { Id = scope + "/" + o.InstanceGuid, InstanceGuid = o.InstanceGuid.ToString(), Name = o.Name, NickName = o.NickName,
                        Type = o.GetType().FullName, ComponentGuid = o.ComponentGuid.ToString(), Assembly = o.GetType().Assembly.GetName().FullName,
                        Description = o.Description, Selected = o.Attributes != null && o.Attributes.Selected };
                    graph.Nodes.Add(n);
                    try
                    {
                        n.Canvas=Canvas(o);
                        var scribble=o as GH_Scribble;
                        if(scribble!=null){n.Annotation=scribble.Text;n.AnnotationFont=scribble.Font.ToString();foreach(var corner in scribble.Corners){n.AnnotationCorners.Add(corner.X);n.AnnotationCorners.Add(corner.Y);}}
                        if(o is IGH_Component)n.Script=ScriptExtractor.Read(o,n.Type,()=>ScriptArchive(o));
                        if(n.Script!=null&&n.Script.Status!="captured"&&n.Script.Status!="empty")
                            n.Issues.Add("Source script "+n.Script.Status+" : "+n.Script.Reason);
                        var membership = new HashSet<Guid> { o.InstanceGuid };
                        bool changed;
                        do { changed = false; foreach (var group in groups) if (group.ObjectIDs.Any(membership.Contains) && membership.Add(group.InstanceGuid)) changed = true; } while (changed);
                        foreach (var group in groups.Where(g => membership.Contains(g.InstanceGuid))) { n.Groups.Add(GroupName(group)); n.GroupIds.Add(scope + "/" + group.InstanceGuid); }
                        var active = o as IGH_ActiveObject; n.Locked = active != null && active.Locked;
                        if(active!=null)
                        {
                            try { n.RuntimeErrors.AddRange(active.RuntimeMessages(GH_RuntimeMessageLevel.Error));n.RuntimeWarnings.AddRange(active.RuntimeMessages(GH_RuntimeMessageLevel.Warning));n.RuntimeStatus="cached; no solver run"; }
                            catch(Exception ex){n.RuntimeStatus="unavailable: "+Error(ex);n.Issues.Add("Lecture des messages runtime impossible : "+Error(ex));}
                        }
                        var component = o as IGH_Component;
                        var parameter = o as IGH_Param;
                        if (component != null)
                        {
                            n.Inputs.AddRange(component.Params.Input.Select(p => ReadPort(p, scope, true)));
                            n.Outputs.AddRange(component.Params.Output.Select(p => p.Name));
                            n.OutputPorts.AddRange(component.Params.Output.Select(p => ReadPort(p, scope, false)));
                        }
                        else if (parameter != null)
                        {
                            n.Inputs.Add(ReadPort(parameter, scope, true));
                            n.Outputs.Add(parameter.Name); n.OutputPorts.Add(ReadPort(parameter, scope, false));
                        }
                        var panel = o as GH_Panel; if (panel != null) n.SourceText = panel.UserText;
                        Settings(o, n);
                        var cluster = o as GH_Cluster;
                        n.IsCluster = cluster != null || o.GetType().Name == "GH_Cluster_OBSOLETE";
                        if (n.IsCluster)
                        {
                            var inner = cluster != null ? cluster.Document("") : Property(o, "InternalDocument") as GH_Document;
                            if (inner == null) throw new InvalidOperationException("Cluster protege ou document interne inaccessible. Aucun contenu invente.");
                            if (stack.Count >= 128) throw new InvalidOperationException("Profondeur de securite 128 atteinte : extraction incomplete.");
                            n.Cluster = ReadDocument(inner, n.Id, stack);
                            if (!n.Cluster.Complete) graph.Complete = false;
                            if (cluster != null) Boundaries(cluster, n, n.Id);
                            else throw new InvalidOperationException("Ancien cluster : correspondances externes/internes non disponibles.");
                        }
                        foreach (var p in n.Inputs.Concat(n.OutputPorts))
                            if (new[] { p.Persistent, p.Volatile }.Any(d => d.Status == "partial" || d.Status == "error"))
                            { graph.Complete = false; n.Issues.Add("Valeurs partiellement illisibles sur le port " + p.Name); }
                    }
                    catch (Exception ex) { n.Issues.Add(Error(ex)); graph.Complete = false; }
                }
            }
            finally { stack.Remove(doc); }
            graph.Capture.FinishedAtUtc=DateTime.UtcNow.ToString("o");
            if(graph.Capture.LastSolutionUtc!=Observe(doc).LastUtc||graph.Capture.SolutionState!=doc.SolutionState.ToString())
                graph.Issues.Add("Etat de solution modifie pendant la lecture : instantane non atomique, reexporter apres stabilisation.");
            return graph;
        }
    }
}
