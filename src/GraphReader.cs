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
        private static object Property(object value, string name)
        {
            if (value == null) return null;
            var p = value.GetType().GetProperty(name);
            return p == null ? null : p.GetValue(value, null);
        }
        private static string Error(Exception ex) { return (ex.InnerException ?? ex).Message; }
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
            return ReadDocument(doc, "main", new HashSet<GH_Document>());
        }
        private static Graph ReadDocument(GH_Document doc, string scope, HashSet<GH_Document> stack)
        {
            var graph = new Graph { Document = doc.DisplayName, FilePath = doc.FilePath, Scope = scope, Host = Rhino.RhinoApp.Version.ToString() };
            if (!stack.Add(doc)) { graph.Complete = false; graph.Issues.Add("Reference de document cyclique : contenu deja en cours de lecture."); return graph; }
            try
            {
                var groups = doc.Objects.OfType<GH_Group>().ToList();
                foreach (var group in groups)
                    graph.Groups.Add(new GroupRecord { Id = scope + "/" + group.InstanceGuid, Name = group.NickName, Members = group.ObjectIDs.Select(id => scope + "/" + id).ToList() });
                foreach (var o in doc.Objects.Where(o => !(o is RiRComponent) && !(o is GH_Group)))
                {
                    var n = new Node { Id = scope + "/" + o.InstanceGuid, InstanceGuid = o.InstanceGuid.ToString(), Name = o.Name, NickName = o.NickName,
                        Type = o.GetType().FullName, ComponentGuid = o.ComponentGuid.ToString(), Assembly = o.GetType().Assembly.GetName().FullName,
                        Description = o.Description, Selected = o.Attributes != null && o.Attributes.Selected };
                    graph.Nodes.Add(n);
                    try
                    {
                        var membership = new HashSet<Guid> { o.InstanceGuid };
                        bool changed;
                        do { changed = false; foreach (var group in groups) if (group.ObjectIDs.Any(membership.Contains) && membership.Add(group.InstanceGuid)) changed = true; } while (changed);
                        foreach (var group in groups.Where(g => membership.Contains(g.InstanceGuid))) { n.Groups.Add(group.NickName); n.GroupIds.Add(scope + "/" + group.InstanceGuid); }
                        var active = o as IGH_ActiveObject; n.Locked = active != null && active.Locked;
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
            var ids = new HashSet<string>(graph.Nodes.Select(n => n.Id));
            foreach (var node in graph.Nodes)
                foreach (var wire in node.Inputs.SelectMany(p => p.Sources))
                    if (!ids.Contains(wire.Node) || wire.Output < 0)
                    { graph.Complete = false; node.Issues.Add("Connexion non resolue : " + wire.Node + ", sortie " + wire.Output); }
            return graph;
        }
    }
}
