using System;
using System.IO;
using RiRtoPY;

static class FunctionalBriefTests
{
    static int checks;
    static void Check(bool ok, string name) { checks++; if(!ok) throw new Exception(name); }
    public static void Run(string folder)
    {
        var g=new Graph { Document="Test", Schema="rir-to-python/1" };
        var p=new Node { Id="panel", Type="Grasshopper.Kernel.Special.GH_Panel", Name="Panel", SourceText="Numero de feuille", Groups=new System.Collections.Generic.List<string>{"Feuilles"} }; p.Outputs.Add("Panel");
        var cluster=new Node { Id="cluster", Type="Grasshopper.Kernel.Special.GH_Cluster", Name="Cluster", NickName="Generateur", Groups=null }; cluster.Outputs.Add("View");
        var input=new Port { Name="Number", Access="tree", Mapping="Graft" }; input.Sources.Add(new Wire { Node="panel", Output=0 }); cluster.Inputs.Add(input);
        g.Nodes.Add(p); g.Nodes.Add(cluster);
        string brief=FunctionalBriefEmitter.Emit(g), detail=FunctionalBriefEmitter.Emit(g,true);
        Check(brief.Contains("2 objets"),"unknown graph extracted");
        Check(brief.Contains("Generateur"),"business nickname retained");
        Check(brief.Contains("Feuilles"),"group retained");
        Check(brief.Contains("Numero de feuille"),"panel text retained");
        Check(brief.Contains("logique interne inconnue"),"cluster uncertainty explicit");
        Check(brief.Contains("Ne cherche pas une reproduction"),"rewrite goal explicit");
        Check(!brief.Contains("## Contrats et flux observes (annexe)"),"compact brief by default");
        Check(detail.Contains("N001.Panel") && detail.Contains("mapping=Graft"),"wires and modifiers retained in detailed brief");
        g.Nodes[0].Inputs.Add(new Port { Name="cycle", Access="tree", Mapping="None" });
        g.Nodes[0].Inputs[0].Sources.Add(new Wire { Node="cluster" });
        Check(FunctionalBriefEmitter.Emit(g).Contains("Dossier"),"cycle does not block functional extraction");
        input.Sources[0].Node="missing";
        Check(FunctionalBriefEmitter.Emit(g,true).Contains("source absente"),"missing source reported without blocking");
        string sketch=FunctionalBriefEmitter.Sketch(g);
        Check(sketch.Contains("Canevas") && sketch.Contains("def main(inputs):"),"design scaffold explicit");
        File.WriteAllText(Path.Combine(folder,"functional.sketch.py"),sketch,new System.Text.UTF8Encoding(false));
        Console.WriteLine("PASS: " + checks + " assertions extraction fonctionnelle");
    }
}
