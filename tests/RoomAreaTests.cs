using System;
using System.IO;
using RiRtoPY;

static class RoomAreaTests
{
    static int checks;
    static void Check(bool ok, string name) { checks++; if (!ok) throw new Exception(name); }
    static Graph Fixture()
    {
        string[] types = { "RhinoInside.Revit.GH.Components.Topology.QueryRooms", "Grasshopper.Kernel.Parameters.Param_Surface", "SurfaceComponents.AnalysisComponents.Component_AreaProperties", "MathComponents.OperatorComponents.Component_MassAddition", "Grasshopper.Kernel.Special.GH_Panel" };
        string[] guids = { "5ddcb816-61a3-480f-ac45-67f66beb2e78", "deaf8653-5528-4286-807c-3de8b8dad781", "86b28a7e-94d9-4791-8306-e13e10d5f8d5", "5b850221-b527-4bd6-8c62-e94168cd6efa", "59e0b89a-e487-49f8-bab8-b5bab16be14c" };
        string[] assemblies = { "RhinoInside.Revit.GH", "Grasshopper", "SurfaceComponents", "MathComponents", "Grasshopper" };
        var g = new Graph();
        for (int i=0; i<5; i++)
        {
            var n = new Node { Id="node"+i, Type=types[i], ComponentGuid=guids[i], Assembly=assemblies[i]+", Version=1.35.9698.15636", Name=types[i], Selected=true };
            n.Outputs.Add("Result"); if(i==2 || i==3) n.Outputs.Add("Extra");
            if(i>0) { var p=new Port { Access=i==3 ? "list":"item", Mapping="None" }; p.Sources.Add(new Wire { Node="node"+(i-1) }); n.Inputs.Add(p); }
            g.Nodes.Add(n);
        }
        foreach (string name in new [] {"Point","Placed","Number","Name","Level"})
            g.Nodes[0].Inputs.Add(new Port { Name=name, Access="item", Mapping="None", Optional=true, HasPersistentData=name=="Placed", Literal=name=="Placed" ? "True":null });
        return g;
    }
    public static void Run(string folder)
    {
        var g=Fixture(); Check(PythonEmitter.Problems(g).Count==0,"room profile accepted");
        string code=PythonEmitter.Emit(g); Check(code.Contains("TrimmedSurface") && !code.Contains("raise NotImplementedError(u"),"room profile emitted");
        File.WriteAllText(Path.Combine(folder,"room_areas.py"),code);
        g.Schema="rir-to-python/1"; Check(!RoomAreaEmitter.Matches(g),"legacy missing metadata blocked");
        g=Fixture(); g.Nodes[0].Inputs[0].HasPersistentData=true; Check(!RoomAreaEmitter.Matches(g),"non scalar point blocked");
        g=Fixture(); g.Nodes[0].Inputs[2].Literal="u'test'"; Check(!RoomAreaEmitter.Matches(g),"filter blocked");
        g=Fixture(); g.Nodes[0].Inputs[0].Optional=false; Check(!RoomAreaEmitter.Matches(g),"required input blocked");
        g=Fixture(); g.Nodes[0].Assembly="RhinoInside.Revit.GH, Version=1.36.0.0"; Check(!RoomAreaEmitter.Matches(g),"changed RiR defaults blocked");
        g=Fixture(); g.Nodes[3].Inputs[0].Sources[0].Output=1; Check(!RoomAreaEmitter.Matches(g),"centroid instead of area blocked");
        g=Fixture(); g.Nodes[2].Inputs[0].Mapping="Flatten"; Check(!RoomAreaEmitter.Matches(g),"flatten blocked");
        g=Fixture(); g.Nodes[1].Issues.Add("locked"); Check(!RoomAreaEmitter.Matches(g),"locked blocked");
        g=Fixture(); g.Nodes[1].ComponentGuid="other"; Check(!RoomAreaEmitter.Matches(g),"component identity checked");
        g=Fixture(); g.Nodes[4].Inputs[0].Sources.Add(new Wire { Node="node1" }); Check(!RoomAreaEmitter.Matches(g),"multiple branches blocked");
        g=Fixture(); g.Nodes[0].Inputs.Add(new Port { Name="Phase" }); Check(!RoomAreaEmitter.Matches(g),"explicit phase blocked");
        g=Fixture(); g.Nodes[0].Selected=false; Check(!PythonEmitter.Emit(g).Contains("u\"node0\":"),"selected outputs respected");
        Console.WriteLine("PASS: " + checks + " assertions profil pieces C#");
    }
}
