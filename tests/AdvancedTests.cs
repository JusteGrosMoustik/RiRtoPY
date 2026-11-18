using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RiRtoPY;

namespace RhinoCodePlatform.GH { public interface IScriptComponent { string Text{get;} string LanguageSpec{get;} } }
namespace RhinoCodePlatform.GH.Context { public interface IScriptObject { bool HasExternScript{get;} } }
public class LegacySourceFake
{public string UsingCode{get{return "using System;";}}public string ScriptCode{get{return "a = x + y;";}}public string AdditionalCode{get{return "int helper() { return 1; }";}}public List<string> References=new List<string>{"custom.dll"};}
public class LegacyFake { public object ScriptSource{get{return new LegacySourceFake();}}public string Language{get{return "CS";}} }
public class ArchiveFake { }
public class PythonFake {public bool InputIsPath{get;set;}public bool HiddenCodeInput{get;set;}public string Code{get{return "a = x + 1";}}}
public class ThrowingPythonFake {public bool InputIsPath{get{return false;}}public string Code{get{throw new InvalidOperationException("Source verrouillee");}}}
public class ModernFake : RhinoCodePlatform.GH.IScriptComponent,RhinoCodePlatform.GH.Context.IScriptObject
{public bool External;string RhinoCodePlatform.GH.IScriptComponent.Text{get{return "a = x * 2";}}string RhinoCodePlatform.GH.IScriptComponent.LanguageSpec{get{return "Python 3";}}bool RhinoCodePlatform.GH.Context.IScriptObject.HasExternScript{get{return External;}}}
static class AdvancedTests
{
    static int checks;
    static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    static Node N(string id,int inputs,int outputs)
    {var n=new Node{Id=id,Name=id,NickName=id,Type="Test"};for(int i=0;i<inputs;i++)n.Inputs.Add(new Port{Name="in"+i});for(int i=0;i<outputs;i++)n.Outputs.Add("out"+i);return n;}
    static void Wire(Node source,int output,Node target,int input){target.Inputs[input].Sources.Add(new Wire{Node=source.Id,Output=output});}
    static Graph Fixture()
    {
        var graph=new Graph{Document="Fixture.gh",Scope="main"};var a=N("main/a",0,1);var b=N("main/b",0,1);var c=N("main/c",2,2);c.IsCluster=true;c.Cluster=new Graph{Scope=c.Id};
        var ia=N(c.Id+"/ia",1,1);var ib=N(c.Id+"/ib",1,1);var oa=N(c.Id+"/oa",1,1);var ob=N(c.Id+"/ob",1,1);
        Wire(ia,0,oa,0);Wire(ib,0,ob,0);c.Cluster.Nodes.AddRange(new[]{ia,ib,oa,ob});
        c.BoundaryLinks.AddRange(new[]{new BoundaryLink{Direction="input",ExternalPort=0,InternalNode=ia.Id},new BoundaryLink{Direction="input",ExternalPort=1,InternalNode=ib.Id},new BoundaryLink{Direction="output",ExternalPort=0,InternalNode=oa.Id},new BoundaryLink{Direction="output",ExternalPort=1,InternalNode=ob.Id}});
        Wire(a,0,c,0);Wire(b,0,c,1);graph.Nodes.AddRange(new[]{a,b,c});GraphAnalysis.Audit(graph);return graph;
    }
    public static void Run(string folder)
    {
        var cs=ScriptExtractor.Read(new LegacyFake(),"ScriptComponents.Component_CSNET_Script",null);
        Check(cs.Status=="captured"&&cs.Parts.Count==3,"legacy source all sections");Check(cs.References.Single()=="custom.dll","script references");
        var vb=ScriptExtractor.Read(new ArchiveFake(),"ScriptComponents.Legacy.ComponentLegacyVbScript",()=>new Dictionary<string,string>{{"ScriptSource","a = x + y"},{"AdditionalSource","Function f()\nEnd Function"}});
        Check(vb.Status=="captured"&&vb.Language=="VB.NET"&&vb.Parts.Count==2,"legacy VB archive fallback");
        var py=ScriptExtractor.Read(new PythonFake{HiddenCodeInput=true},"GhPython.Component.ZuiPythonComponent",null);Check(py.Status=="captured"&&py.Parts[0].Text=="a = x + 1","python embedded source");
        Check(ScriptExtractor.Read(new PythonFake{InputIsPath=true},"GhPython.Component.ZuiPythonComponent",null).Reason.Contains("externe"),"python external source not read");
        Check(ScriptExtractor.Read(new PythonFake(),"GhPython.Component.ZuiPythonComponent",null).Status=="partial","python wire-driven source explicit");
        var modern=ScriptExtractor.Read(new ModernFake(),"RhinoCodePluginGH.Components.ScriptComponent",null);Check(modern.Status=="captured"&&modern.Language=="Python 3","modern explicit interface adapter");
        Check(ScriptExtractor.Read(new ModernFake{External=true},"RhinoCodePluginGH.Components.ScriptComponent",null).Status=="unavailable","modern external source explicit");
        Check(ScriptExtractor.Read(new ThrowingPythonFake(),"GhPython.Component.PythonComponent",null).Reason=="Source verrouillee","source read exception explicit");
        Check(ScriptExtractor.Read(new ArchiveFake(),"Other.CustomScript",null).Reason.Contains("adaptateur"),"unknown script explicit reason");
        Check(ScriptExtractor.Read(new ArchiveFake(),"Standard.Math",null)==null,"regular nodes not scripts");
        var empty=ScriptExtractor.Read(new ArchiveFake(),"ScriptComponents.Component_CSNET_Script",()=>new Dictionary<string,string>{{"ScriptSource",""}});Check(empty.Status=="empty","empty code distinct from unavailable");
        var graph=Fixture();Check(graph.Complete&&graph.Diagnostics.Count==0,"valid graph and complete boundaries");
        var slice=GraphAnalysis.Dependencies(graph,"main/c",0);
        Check(slice.Nodes.Contains("main/a")&&!slice.Nodes.Contains("main/b"),"output-specific cluster dependency excludes unrelated input");
        Check(slice.Nodes.Contains("main/c/oa")&&!slice.Nodes.Contains("main/c/ob"),"output-specific internal branch");
        var focused=GraphAnalysis.Project(graph,slice);Check(focused.IsPartial&&focused.Complete,"projection retains coherent scope");Check(!GraphReportEmitter.AllNodes(focused).Any(n=>n.Id=="main/b"),"projection removes unrelated objects");
        var deep=Fixture();var parent=deep.Nodes[2];var middle=N(parent.Id+"/nested",1,1);middle.IsCluster=true;middle.Cluster=new Graph{Scope=middle.Id};
        var enter=N(middle.Id+"/enter",1,1);var leave=N(middle.Id+"/leave",1,1);Wire(enter,0,leave,0);middle.Cluster.Nodes.AddRange(new[]{enter,leave});
        middle.BoundaryLinks.Add(new BoundaryLink{Direction="input",ExternalPort=0,InternalNode=enter.Id});middle.BoundaryLinks.Add(new BoundaryLink{Direction="output",ExternalPort=0,InternalNode=leave.Id});
        Wire(parent.Cluster.Nodes[0],0,middle,0);parent.Cluster.Nodes[2].Inputs[0].Sources.Clear();Wire(middle,0,parent.Cluster.Nodes[2],0);parent.Cluster.Nodes.Add(middle);
        var deepSlice=GraphAnalysis.Dependencies(deep,parent.Id,0);Check(deepSlice.Nodes.Contains(enter.Id)&&deepSlice.Nodes.Contains(leave.Id)&&deepSlice.Nodes.Contains("main/a")&&!deepSlice.Nodes.Contains("main/b"),"dependencies traverse nested cluster boundaries");
        var deepProjection=GraphAnalysis.Project(deep,deepSlice);Check(deepProjection.Complete,"nested focused scope remains coherent");
        var beforeHint=Fixture();var afterHint=GraphAnalysis.Clone(beforeHint);afterHint.Nodes[2].Inputs[0].TypeHint="float";Check(GraphAnalysis.Compare(beforeHint,afterHint).Contains("Parametres"),"type hint change affects conversion contract");
        var g=GraphAnalysis.Clone(graph);var c=g.Nodes[2];c.Cluster.Nodes[0].Inputs[0].Sources.Add(new Wire{Node=c.Cluster.Nodes[2].Id,Output=0});
        Check(GraphAnalysis.Dependencies(g,"main/c",0).VisitedPorts<50,"dependency cycle terminates");
        g.Nodes[0].RuntimeErrors.Add("bad result");GraphAnalysis.Audit(g);Check(g.Complete&&g.Diagnostics.Any(d=>d.Category=="runtime"),"runtime error does not imply extraction failure");
        g.Nodes[2].Cluster.Nodes[0].Issues.Add("unreadable");GraphAnalysis.Audit(g);Check(!g.Complete&&g.Diagnostics.Any(d=>d.Node=="main/c/ia"&&d.Category=="extraction"),"nested extraction error aggregated globally");
        int count=g.Diagnostics.Count;GraphAnalysis.Audit(g);Check(g.Diagnostics.Count==count,"audit repeat does not duplicate diagnostics");
        g=GraphAnalysis.Clone(graph);g.Nodes[2].Inputs[0].Sources[0].Output=2;GraphAnalysis.Audit(g);Check(g.Diagnostics.Any(d=>d.Category=="coherence"&&d.Message.Contains("Indice")),"out-of-range output detected");
        g=GraphAnalysis.Clone(graph);g.Nodes[2].Inputs[0].Sources[0].Node="missing";GraphAnalysis.Audit(g);Check(!g.Complete&&g.Diagnostics.Any(d=>d.Category=="coherence"),"missing node detected");
        g=GraphAnalysis.Clone(graph);g.Nodes[2].BoundaryLinks[0].ExternalPort=99;GraphAnalysis.Audit(g);Check(g.Diagnostics.Any(d=>d.Message.Contains("Passage")),"invalid cluster mapping detected");
        g=GraphAnalysis.Clone(graph);g.Nodes.Add(N(g.Nodes[0].Id,0,1));GraphAnalysis.Audit(g);Check(g.Diagnostics.Any(d=>d.Message.Contains("duplique")),"duplicate ids detected");
        graph.FilePath=@"\\server\confidential\file.gh";graph.Nodes[0].SourceText="SECRET_VALUE";graph.Nodes[0].Settings.Add("token=SECRET_VALUE");
        graph.Nodes[0].Inputs.Add(new Port{Expression="SECRET_VALUE",Persistent=new DataSnapshot{Status="captured",ItemCount=1,Branches={new DataBranch{Path="{0}",Values={new DataValue{Text="SECRET_VALUE"}}}}}});
        graph.Nodes[1].Script=modern;modern.Parts[0].Text="file = @\"C:\\company\\secret.py\"";
        var privacy=ExportPrivacy.Apply(graph,new PrivacyOptions());
        Check(privacy.FilePath=="[CHEMIN MASQUE]","network path masked");Check(!privacy.Nodes[1].Script.Parts[0].Text.Contains("company"),"paths inside script masked");Check(graph.FilePath.Contains("confidential"),"privacy copy leaves source graph intact");
        var redacted=ExportPrivacy.Apply(graph,new PrivacyOptions{ExcludeValues=true,ExcludeScripts=true});
        using(var stream=new MemoryStream()){GraphReportEmitter.WriteJson(stream,redacted);var json=Encoding.UTF8.GetString(stream.ToArray());Check(!json.Contains("SECRET_VALUE")&&!json.Contains("secret.py"),"values and code excluded from actual JSON");}
        Check(ExportPrivacy.Mask(@"C:\Users\name\file.gh")=="[CHEMIN MASQUE]","local path masked");Check(ExportPrivacy.Mask("/home/user/file.gh")=="[CHEMIN MASQUE]","unix path masked");
        Check(redacted.Nodes[1].Script.Status=="redacted","intentional source exclusion explicit");
        var baseline=Fixture();var after=GraphAnalysis.Clone(baseline);after.Nodes[0].Settings.Add("CurrentValue = 2");
        string diff=GraphAnalysis.Compare(baseline,after);Check(diff.Contains("Parametres")&&!diff.Contains("Cablage modifie"),"parameter change distinguished from wiring");
        after=GraphAnalysis.Clone(baseline);after.Nodes[2].Inputs[0].Sources[0].Node="main/b";diff=GraphAnalysis.Compare(baseline,after);Check(diff.Contains("Cablage modifie"),"wiring diff");
        after=GraphAnalysis.Clone(baseline);after.Nodes[0].Inputs.Add(new Port{Volatile=new DataSnapshot{Status="empty"}});diff=GraphAnalysis.Compare(baseline,after);Check(diff.Contains("Instantane des valeurs"),"volatile-only snapshots category");
        Check(GraphAnalysis.Compare(baseline,baseline).Contains("Aucun changement"),"identical capture comparison");
        after=GraphAnalysis.Clone(baseline);after.Nodes[0].Annotation="NOTE";after.Nodes[0].Canvas=new CanvasInfo{X=12,Y=24};diff=GraphAnalysis.Compare(baseline,after);Check(diff.Contains("position")&&diff.Contains("annotations"),"annotation and canvas diff");
        graph.Nodes[0].Annotation="Intention de conception";graph.Nodes[0].Canvas=new CanvasInfo{X=10,Y=20};graph.Capture.CapturedAtUtc="2026-11-18T16:00:00Z";
        graph.Nodes[0].OutputPorts.Add(new Port{Volatile=new DataSnapshot{Status="captured",Branches={new DataBranch{Values={new DataValue{Geometry=new GeometrySummary{Status="captured",Type="Curve",Properties={"Length=12","Radius=5"}}}}}}}});
        GraphAnalysis.Audit(graph);
        string full=GraphReportEmitter.Emit(graph),functional=CompactReport.Emit(graph,ReportLevel.Functional),overview=CompactReport.Emit(graph,ReportLevel.Overview);
        Check(full.Contains("Intention de conception")&&full.Contains("pivot=[10,20]"),"exhaustive annotations and layout");
        Check(full.Contains("Source / script")&&full.Contains("Python 3"),"source exported in exhaustive report");
        Check(full.Contains("Radius=5"),"geometry properties rendered");Check(functional.Contains("Interfaces candidates pour les fonctions Python"),"conversion contracts present in default report");Check(functional.Contains("Annotations et scripts")&&functional.Contains("Hypothese"),"functional report labels inference and scripts");
        Check(overview.Length<functional.Length,"overview smaller than functional");
        graph.Nodes[0].SourceText=new string('x',100000);full=GraphReportEmitter.Emit(graph);functional=CompactReport.Emit(graph,ReportLevel.Functional);Check(functional.Length<full.Length/2,"functional truncation materially reduces volume");Check(functional.Contains("limite")&&full.Contains(new string('x',100000)),"excerpts explicit and exhaustive text intact");
        File.WriteAllText(Path.Combine(folder,"example.functional.md"),functional,new UTF8Encoding(false));File.WriteAllText(Path.Combine(folder,"example.overview.md"),overview,new UTF8Encoding(false));
        Console.WriteLine("PASS: "+checks+" assertions avancees.");
    }
}
