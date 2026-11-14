using System;
using System.IO;
using RiRtoPY;

static class GeneralRevitTests
{
    static int checks;
    static void Check(bool ok, string name) { checks++; if(!ok) throw new Exception(name); }
    static Port Input(string name, string source, int output, string access)
    { var p=new Port { Name=name, Access=access, Mapping="None" }; p.Sources.Add(new Wire { Node=source, Output=output }); return p; }
    static Node Node(string id, string type, string guid, params string[] outputs)
    { var n=new Node { Id=id, Name=id, Type="RhinoInside.Revit.GH.Components."+type, ComponentGuid=guid, Assembly="RhinoInside.Revit.GH, Version=1.35.9698.15636", Selected=true }; n.Outputs.AddRange(outputs); return n; }
    static Graph Fixture()
    {
        var g=new Graph();
        var source=new Node { Id="categories", Name="Categories", Type="RhinoInside.Revit.GH.Parameters.Input.BuiltInCategories" }; source.Outputs.Add("Categories"); g.Nodes.Add(source);
        var filter=Node("filter","Filters.ElementCategoryFilter","d08f7ab1-be36-45fa-b006-0078022db140","Filter");
        filter.Inputs.Add(Input("Categories","categories",0,"list")); filter.Inputs.Add(new Port { Name="Inverted", Literal="False", Access="item", Mapping="None", HasPersistentData=true }); g.Nodes.Add(filter);
        var query=Node("query","Elements.QueryElements","0f7da57e-6c05-4dd0-aabf-69e42df38859","Elements","Count"); query.Inputs.Add(Input("Filter","filter",0,"item")); query.Inputs.Add(new Port { Name="Limit", Literal="1", Access="item", Mapping="None", HasPersistentData=true }); g.Nodes.Add(query);
        var category=Node("category","Elements.ElementPropertyCategory","5ac48de6-f706-4e88-a4ad-7a4439f1dab5","Element","Category"); category.Inputs.Add(Input("Element","query",0,"item")); g.Nodes.Add(category);
        var name=Node("name","Elements.ElementPropertyName","01934ad1-f31b-43e5-add9-c196f4a2467e","Element","Name"); name.Inputs.Add(Input("Element","query",0,"item")); name.Inputs.Add(new Port { Name="Name", Access="item", Mapping="None", Optional=true }); g.Nodes.Add(name);
        g.Nodes.Add(Node("active", "Documents.ActiveDocument", "ee033516-c1dc-4c72-8fcd-f85f38a0f267", "Active Document"));
        return g;
    }
    public static void Run(string folder)
    {
        var g=Fixture(); Check(PythonEmitter.Problems(g).Count==0,"general chain accepted");
        string code=PythonEmitter.Emit(g); Check(code.Contains("def query_elements") && code.Contains("map_element(lambda e:"),"general self-contained source emitted");
        Check(code.Contains("inputs[u\"categories\"]"),"unknown picker values explicit");
        Check(code.Contains("scalar_input(document)"),"caller document propagated");
        File.WriteAllText(Path.Combine(folder,"general_revit.py"),code,new System.Text.UTF8Encoding(false));
        g=Fixture(); g.Schema="rir-to-python/1"; Check(PythonEmitter.Problems(g).Count>0,"legacy schema blocked");
        g=Fixture(); g.Nodes[4].Inputs[1].Literal="u'rename'"; Check(PythonEmitter.Problems(g).Count>0,"name write blocked");
        g=Fixture(); g.Nodes[2].Inputs[0].Mapping="Graft"; Check(PythonEmitter.Problems(g).Count>0,"tree modifier blocked");
        g=Fixture(); g.Nodes[3].Inputs[0].Sources[0].Output=9; Check(PythonEmitter.Problems(g).Count>0,"bad output index blocked");
        g=Fixture(); g.Nodes[3].Assembly="RhinoInside.Revit.GH, Version=1.36.0.0"; Check(PythonEmitter.Problems(g).Count>0,"signature version gated");
        g=Fixture(); g.Nodes[3].ComponentGuid="wrong"; Check(PythonEmitter.Problems(g).Count>0,"GUID verified");
        g=Fixture(); g.Nodes[1].Inputs[0].Sources.Clear(); Check(PythonEmitter.Problems(g).Count>0,"missing categories blocked");
        g=Fixture(); g.Nodes[0].Id=g.Nodes[1].Id; Check(PythonEmitter.Problems(g).Count>0,"duplicate IDs blocked");
        Console.WriteLine("PASS: " + checks + " assertions C# general Revit");
    }
}
