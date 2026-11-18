using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RiRtoPY
{
    public sealed class DependencySlice
    {
        public HashSet<string> Nodes=new HashSet<string>();
        public string Target;
        public int VisitedPorts;
    }
    public static class GraphAnalysis
    {
        public static IEnumerable<Graph> Documents(Graph graph)
        { yield return graph;foreach(var n in graph.Nodes)if(n.Cluster!=null)foreach(var doc in Documents(n.Cluster))yield return doc; }
        public static void Normalize(Graph graph)
        {
            graph.Nodes=graph.Nodes??new List<Node>();graph.Groups=graph.Groups??new List<GroupRecord>();graph.Issues=graph.Issues??new List<string>();graph.ExportNotes=graph.ExportNotes??new List<string>();graph.Capture=graph.Capture??new CaptureState();
            graph.Diagnostics=graph.Diagnostics??new List<Diagnostic>();
            foreach(var n in graph.Nodes)
            {
                n.Inputs=n.Inputs??new List<Port>();n.OutputPorts=n.OutputPorts??new List<Port>();n.Outputs=n.Outputs??new List<string>();n.Groups=n.Groups??new List<string>();n.GroupIds=n.GroupIds??new List<string>();n.Settings=n.Settings??new List<string>();n.Issues=n.Issues??new List<string>();n.BoundaryLinks=n.BoundaryLinks??new List<BoundaryLink>();n.RuntimeErrors=n.RuntimeErrors??new List<string>();n.RuntimeWarnings=n.RuntimeWarnings??new List<string>();n.AnnotationCorners=n.AnnotationCorners??new List<double>();
                foreach(var p in n.Inputs.Concat(n.OutputPorts))p.Sources=p.Sources??new List<Wire>();
                if(n.Cluster!=null)Normalize(n.Cluster);
            }
        }
        private static void Add(Graph graph,string category,string severity,string node,string message)
        { if(!graph.Diagnostics.Any(d=>d.Category==category&&d.Severity==severity&&d.Node==node&&d.Message==message))graph.Diagnostics.Add(new Diagnostic{Category=category,Severity=severity,Node=node,Message=message}); }
        public static void Audit(Graph graph)
        {
            Normalize(graph);var docs=Documents(graph).ToList();
            foreach(var doc in docs)
            {
                doc.Diagnostics.Clear();
                foreach(var issue in doc.Issues)Add(doc,"extraction","error",doc.Scope,issue);
                var byId=doc.Nodes.GroupBy(n=>n.Id).ToDictionary(g=>g.Key,g=>g.First());
                foreach(var group in doc.Nodes.GroupBy(n=>n.Id).Where(g=>g.Count()>1))Add(doc,"coherence","error",group.Key,"Identifiant duplique.");
                foreach(var n in doc.Nodes)
                {
                    foreach(var issue in n.Issues)Add(doc,"extraction","error",n.Id,issue);
                    foreach(var error in n.RuntimeErrors)Add(doc,"runtime","error",n.Id,error);
                    foreach(var warning in n.RuntimeWarnings)Add(doc,"runtime","warning",n.Id,warning);
                    if(n.IsCluster&&n.Cluster==null&&!n.Issues.Any())Add(doc,"extraction","error",n.Id,"Interieur du cluster inaccessible, sans raison disponible.");
                    if(n.Script!=null&&n.Script.Status!="captured"&&n.Script.Status!="empty"&&n.Script.Status!="redacted"&&!n.Issues.Any(i=>i.StartsWith("Source script")))Add(doc,"extraction","error",n.Id,"Script "+n.Script.Status+" : "+n.Script.Reason);
                    foreach(var wire in n.Inputs.SelectMany(p=>p.Sources))
                    {
                        Node source;
                        if(!byId.TryGetValue(wire.Node,out source))Add(doc,"coherence","error",n.Id,"Source absente : "+wire.Node);
                        else if(wire.Output<0||wire.Output>=source.Outputs.Count)Add(doc,"coherence","error",n.Id,"Indice de sortie invalide : "+wire.Node+"["+wire.Output+"].");
                    }
                    foreach(var p in n.Inputs.Concat(n.OutputPorts))foreach(var data in new[]{p.Persistent,p.Volatile}.Where(d=>d!=null))
                    {
                        if(data.Status=="error"||data.Status=="partial")Add(doc,"extraction","error",n.Id,"Donnees "+p.Name+" : "+data.Status+"; "+data.Error);
                        foreach(var value in data.Branches.SelectMany(b=>b.Values))if(value.Geometry!=null&&(value.Geometry.Status=="partial"||value.Geometry.Status=="unavailable"))Add(doc,"extraction","warning",n.Id,"Geometrie : "+value.Geometry.Reason);
                    }
                    if(n.Cluster!=null)
                    {
                        var childIds=new HashSet<string>(n.Cluster.Nodes.Select(c=>c.Id));
                        foreach(var link in n.BoundaryLinks)
                            if((link.Direction!="input"&&link.Direction!="output")||link.ExternalPort<0||link.ExternalPort>=(link.Direction=="input"?n.Inputs.Count:n.Outputs.Count)||!childIds.Contains(link.InternalNode))Add(doc,"coherence","error",n.Id,"Passage de cluster invalide.");
                        if(!doc.IsPartial&&n.BoundaryLinks.Count!=n.Inputs.Count+n.Outputs.Count)Add(doc,"coherence","error",n.Id,"Correspondances de cluster incompletes : "+n.BoundaryLinks.Count+" pour "+(n.Inputs.Count+n.Outputs.Count)+" ports.");
                    }
                }
            }
            // Aggregate all descendant diagnostics in each ancestor, without changing runtime into extraction errors.
            Aggregate(graph);
            int alias=0;foreach(var node in GraphReportEmitter.AllNodes(graph))node.ExportAlias="N"+(++alias).ToString("0000");
        }
        private static void Aggregate(Graph graph)
        {
            foreach(var n in graph.Nodes.Where(n=>n.Cluster!=null)){Aggregate(n.Cluster);graph.Diagnostics.AddRange(n.Cluster.Diagnostics);}
            graph.Complete=!graph.Diagnostics.Any(d=>d.Severity=="error"&&d.Category!="runtime");
        }
        private static string Key(string id,string side,int port){return id+"|"+side+"|"+port;}
        public static DependencySlice Dependencies(Graph graph,string target,int output)
        {
            Normalize(graph);var all=GraphReportEmitter.AllNodes(graph).ToDictionary(n=>n.Id);
            Node selected;if(!all.TryGetValue(target,out selected)||output<0||output>=selected.Outputs.Count)throw new ArgumentException("Sortie cible introuvable.");
            var reverse=new Dictionary<string,List<string>>();var owners=new Dictionary<string,string>();
            Action<string,string> edge=(from,to)=>{if(!reverse.ContainsKey(to))reverse[to]=new List<string>();reverse[to].Add(from);};
            foreach(var n in all.Values)
            {
                for(int i=0;i<n.Inputs.Count;i++)
                {
                    string input=Key(n.Id,"in",i);owners[input]=n.Id;
                    foreach(var source in n.Inputs[i].Sources)edge(Key(source.Node,"out",source.Output),input);
                    // Conservative component contract: any input may influence any output.
                    if(n.Cluster==null)for(int j=0;j<n.Outputs.Count;j++)edge(input,Key(n.Id,"out",j));
                }
                for(int j=0;j<n.Outputs.Count;j++)owners[Key(n.Id,"out",j)]=n.Id;
                foreach(var link in n.BoundaryLinks)
                    if(link.Direction=="input")edge(Key(n.Id,"in",link.ExternalPort),Key(link.InternalNode,"out",0));
                    else edge(Key(link.InternalNode,"in",0),Key(n.Id,"out",link.ExternalPort));
                // Missing mappings: retain all inputs rather than falsely exclude dependencies.
                if(n.Cluster!=null&&n.BoundaryLinks.Count!=n.Inputs.Count+n.Outputs.Count)
                    for(int i=0;i<n.Inputs.Count;i++)for(int j=0;j<n.Outputs.Count;j++)edge(Key(n.Id,"in",i),Key(n.Id,"out",j));
            }
            var result=new DependencySlice{Target=target+".sortie["+output+"]"};var queue=new Queue<string>();var seen=new HashSet<string>();queue.Enqueue(Key(target,"out",output));
            while(queue.Count>0)
            {
                var key=queue.Dequeue();if(!seen.Add(key))continue;string owner;if(owners.TryGetValue(key,out owner))result.Nodes.Add(owner);
                List<string> sources;if(reverse.TryGetValue(key,out sources))foreach(var source in sources)queue.Enqueue(source);
            }
            result.VisitedPorts=seen.Count;
            // Include containers so scoped internal objects remain interpretable.
            foreach(var n in all.Values.Where(n=>n.Cluster!=null).OrderByDescending(n=>n.Id.Length))if(GraphReportEmitter.AllNodes(n.Cluster).Any(c=>result.Nodes.Contains(c.Id)))result.Nodes.Add(n.Id);
            return result;
        }
        public static Graph ReadJson(Stream stream)
        {
            var graph=(Graph)new DataContractJsonSerializer(typeof(Graph),new DataContractJsonSerializerSettings{MaxItemsInObjectGraph=Int32.MaxValue}).ReadObject(stream);
            if(graph==null||!(graph.Schema??"").StartsWith("grasshopper-analysis/"))throw new InvalidDataException("Schema de capture non reconnu.");Normalize(graph);return graph;
        }
        public static Graph Clone(Graph graph)
        { using(var stream=new MemoryStream()){GraphReportEmitter.WriteJson(stream,graph);stream.Position=0;return ReadJson(stream);} }
        public static Graph Project(Graph graph,DependencySlice slice)
        {
            var included=new HashSet<string>(slice.Nodes);var contextGroups=new HashSet<string>(GraphReportEmitter.AllNodes(graph).Where(n=>included.Contains(n.Id)).SelectMany(n=>n.GroupIds));
            foreach(var n in GraphReportEmitter.AllNodes(graph).Where(n=>n.Annotation!=null&&n.GroupIds.Any(contextGroups.Contains)))included.Add(n.Id);
            var clone=Clone(graph);Filter(clone,included);clone.ExportNotes.Add("Perimetre : dependances amont de "+slice.Target+". Dependances conservatrices au sein des composants ; aucun ordre d'execution prouve.");Audit(clone);return clone;
        }
        private static void Filter(Graph graph,HashSet<string> ids)
        {
            graph.IsPartial=true;graph.Nodes.RemoveAll(n=>!ids.Contains(n.Id));
            foreach(var n in graph.Nodes)
            {
                // Retained inputs may belong to an unused cluster output. Preserve their existence,
                // but remove excluded wires and state this explicitly in ExportNotes.
                foreach(var p in n.Inputs)p.Sources.RemoveAll(w=>!ids.Contains(w.Node));
                if(n.Cluster!=null)Filter(n.Cluster,ids);
                n.BoundaryLinks.RemoveAll(l=>!ids.Contains(l.InternalNode));
            }
            foreach(var group in graph.Groups)group.Members.RemoveAll(id=>!ids.Contains(id)&&!graph.Groups.Any(g=>g.Id==id));
            graph.Groups.RemoveAll(g=>g.Members.Count==0);
        }
        public static string Compare(Graph before,Graph after)
        {
            Normalize(before);Normalize(after);var a=GraphReportEmitter.AllNodes(before).ToDictionary(n=>n.Id);var b=GraphReportEmitter.AllNodes(after).ToDictionary(n=>n.Id);
            var text=new StringBuilder("# Comparaison de captures Grasshopper\n\nCorrespondance par identifiants scopes ; aucune equivalence si les GUID ont ete regeneres.\nLes captures a confidentialite/perimetre differents peuvent masquer des changements.\n");
            text.AppendLine("Avant : "+before.Capture.CapturedAtUtc+" ; apres : "+after.Capture.CapturedAtUtc);
            if(before.Schema!=after.Schema)text.AppendLine("Schemas differents : les metadonnees nouvellement capturees ne prouvent pas une modification du fichier Grasshopper.");
            if(before.Capture.DocumentId!=after.Capture.DocumentId)text.AppendLine("Attention : identifiants de document differents ou absents ; correspondance a confirmer.");
            foreach(var id in b.Keys.Except(a.Keys))text.AppendLine("- Ajoute : "+id+" / "+b[id].Name);
            foreach(var id in a.Keys.Except(b.Keys))text.AppendLine("- Supprime : "+id+" / "+a[id].Name);
            foreach(var id in a.Keys.Intersect(b.Keys))
            {
                var x=a[id];var y=b[id];
                if(Wires(x)!=Wires(y))text.AppendLine("- Cablage modifie : "+id);
                if(Signature(x,false)!=Signature(y,false))text.AppendLine("- Parametres / code / annotations modifies : "+id);
                if(Signature(x,true)!=Signature(y,true))text.AppendLine("- Instantane des valeurs volatiles modifie : "+id+" (ne prouve pas un nouveau calcul)");
                if(x.Name!=y.Name||x.NickName!=y.NickName||String.Join("|",x.GroupIds)!=String.Join("|",y.GroupIds)||CanvasKey(x.Canvas)!=CanvasKey(y.Canvas))text.AppendLine("- Organisation / noms / position modifies : "+id);
                if(String.Join("|",x.RuntimeErrors.Concat(x.RuntimeWarnings))!=String.Join("|",y.RuntimeErrors.Concat(y.RuntimeWarnings)))text.AppendLine("- Messages runtime modifies : "+id);
            }
            foreach(var doc in Documents(after))
            {
                var old=Documents(before).FirstOrDefault(d=>d.Scope==doc.Scope);
                if(old==null||GroupsKey(old)!=GroupsKey(doc))text.AppendLine("- Groupes / disposition modifies : "+doc.Scope);
            }
            if(!text.ToString().Contains("- "))text.AppendLine("Aucun changement detecte dans les categories comparees.");return text.ToString();
        }
        private static string CanvasKey(CanvasInfo c){return c==null?"":String.Join(";",new[]{c.X,c.Y,c.Left,c.Top,c.Width,c.Height}.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));}
        private static string GroupsKey(Graph g){return String.Join("\n",g.Groups.OrderBy(x=>x.Id).Select(x=>x.Id+"|"+x.Name+"|"+x.Colour+"|"+CanvasKey(x.Canvas)+"|"+String.Join(",",x.Members)));}
        private static string Wires(Node n){return String.Join("\n",n.Inputs.SelectMany((p,i)=>p.Sources.Select((w,j)=>i+":"+j+":"+w.Node+":"+w.Output)).Concat(n.BoundaryLinks.Select(l=>l.Direction+":"+l.ExternalPort+":"+l.InternalNode)));}
        private static string Signature(Node n,bool volatileData)
        {
            var record=volatileData?new Node { Inputs=n.Inputs.Select(p=>new Port{Volatile=p.Volatile}).ToList(),OutputPorts=n.OutputPorts.Select(p=>new Port{Volatile=p.Volatile}).ToList() }:new Node { Type=n.Type,Settings=n.Settings,SourceText=n.SourceText,Annotation=n.Annotation,Script=n.Script,Locked=n.Locked,Outputs=n.Outputs,Inputs=n.Inputs.Select(p=>new Port{Name=p.Name,Access=p.Access,TypeHint=p.TypeHint,Locked=p.Locked,Optional=p.Optional,Mapping=p.Mapping,Reverse=p.Reverse,Simplify=p.Simplify,Expression=p.Expression,Persistent=p.Persistent}).ToList() };
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(Node),new DataContractJsonSerializerSettings{MaxItemsInObjectGraph=Int32.MaxValue}).WriteObject(stream,record);
                using(var hash=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));
            }
        }
    }
}
