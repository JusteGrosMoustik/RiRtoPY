using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using RiRtoPY;

class CoreTests
{
    static int assertions;
    static void Check(bool condition, string name)
    { assertions++; if (!condition) throw new Exception("FAIL: " + name); }
    static Node Constant(string id, string literal)
    { var n = new Node { Id=id, Name=id, NickName=id, Operation="constant", Literal=literal }; n.Outputs.Add("N"); return n; }
    static Port Input(string id)
    { var p = new Port { Name="N", Access="item", Mapping="None" }; p.Sources.Add(new Wire { Node=id, Output=0 }); return p; }
    static Graph Fixture()
    {
        var result = new Node { Id="result", Name="Division", NickName="Resultat", Selected=true, Operation="divide" };
        result.Inputs.Add(Input("a")); result.Inputs.Add(Input("b")); result.Outputs.Add("R");
        var g = new Graph(); g.Nodes.Add(result); g.Nodes.Add(Constant("a","7")); g.Nodes.Add(Constant("b","2")); return g;
    }
    static void Main(string[] args)
    {
        var graph = Fixture();
        Check(PythonEmitter.Ordered(graph).Last().Id == "result", "dependency ordering");
        Check(PythonEmitter.Problems(graph).Count == 0, "scalar graph accepted");
        string python = PythonEmitter.Emit(graph);
        Check(python.Contains("float(v0) / float(v1)"), "true division");
        Check(python.Contains("inputs.get(u\"a\", 7)"), "exposed slider default");
        Check(PythonEmitter.ScalarLiteral(Double.NaN) == null, "NaN rejected");
        Check(PythonEmitter.ScalarLiteral(Double.PositiveInfinity) == null, "infinity rejected");
        Check(PythonEmitter.ScalarLiteral(true) == "True", "boolean");
        Check(PythonEmitter.Quote("a\n\"\\é").Contains("\\u00e9"), "escaped unicode");
        Check(PythonEmitter.ScalarLiteral(1.5) == "1.5", "invariant decimal separator");
        Check(UpdateService.IsNewer("v0.2.0", "0.1.0"), "new update");
        Check(!UpdateService.IsNewer("v0.1.0", "0.1.0"), "same version");
        Check(!UpdateService.IsNewer("v0.0.9", "0.1.0"), "older version");
        Check(!UpdateService.IsNewer("v0.2.0-beta", "0.1.0"), "prerelease excluded");
        Check(!UpdateService.IsNewer("invalid", "0.1.0"), "malformed version");
        graph.Nodes[0].Inputs[0].Mapping="Flatten";
        Check(PythonEmitter.Emit(graph).Contains("raise NotImplementedError"), "flatten blocks export");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Access="list";
        Check(PythonEmitter.Problems(graph).Count>0, "list blocks export");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Expression="x*2";
        Check(PythonEmitter.Problems(graph).Count>0, "expression blocks export");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Sources.Add(new Wire {Node="b"});
        Check(PythonEmitter.Problems(graph).Count>0, "multiple sources blocked");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Sources[0].Output=1;
        Check(PythonEmitter.Problems(graph).Count>0, "nonzero port blocked");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Sources[0].Node="absent";
        Check(PythonEmitter.Emit(graph).Contains("raise NotImplementedError"), "missing node blocked");
        graph=Fixture(); graph.Nodes[1].Inputs.Add(Input("result"));
        Check(PythonEmitter.Emit(graph).Contains("Cycle"), "cycle blocked");
        graph=Fixture(); graph.Nodes[0].Operation=null;
        Check(PythonEmitter.Emit(graph).Contains("raise NotImplementedError"), "unknown component blocked");
        graph=Fixture(); graph.Nodes[1].Literal=null;
        Check(PythonEmitter.Problems(graph).Count>0, "unknown literal blocked");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Reverse=true;
        Check(PythonEmitter.Problems(graph).Count>0, "reverse blocked");
        graph=Fixture(); graph.Nodes[0].Inputs[0].Simplify=true;
        Check(PythonEmitter.Problems(graph).Count>0, "simplify blocked");
        graph=Fixture(); graph.Nodes[0].Outputs.Add("extra");
        Check(PythonEmitter.Problems(graph).Count>0, "multiple outputs blocked");
        graph=Fixture(); graph.Nodes[0].Issues.Add("Objet desactive");
        Check(PythonEmitter.Problems(graph).Count>0, "disabled component blocked");
        using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"tag_name\":\"v0.2.0\",\"assets\":[{\"name\":\"setup.exe\",\"browser_download_url\":\"https://github.com/x\"}]}")))
        {
            var r=(Release)new DataContractJsonSerializer(typeof(Release)).ReadObject(ms);
            Check(r.Tag=="v0.2.0" && r.Assets.Single().Name=="setup.exe", "GitHub release deserialization");
        }
        graph=Fixture(); graph.Nodes[0].Operation="unknown";
        Check(PythonEmitter.Emit(graph).Contains("raise NotImplementedError"), "unknown operation blocks safely");
        graph=Fixture(); graph.Nodes[0].Inputs.Clear();
        Check(PythonEmitter.Emit(graph).Contains("raise NotImplementedError"), "division arity blocks safely");
        graph=Fixture(); graph.Nodes[0].Operation="identity";
        Check(PythonEmitter.Emit(graph).Contains("raise NotImplementedError"), "identity arity blocks safely");
        Check(PythonEmitter.Quote("é").StartsWith("u\""), "unicode literals under IronPython 2");
        Directory.CreateDirectory(args[0]);
        RoomAreaTests.Run(args[0]);
        GeneralRevitTests.Run(args[0]);
        File.WriteAllText(Path.Combine(args[0], "division.py"),python,new UTF8Encoding(false));
        graph=Fixture(); graph.Nodes[0].Operation="add";
        File.WriteAllText(Path.Combine(args[0], "addition.py"),PythonEmitter.Emit(graph),new UTF8Encoding(false));
        graph.Nodes[0].Operation="subtract";
        File.WriteAllText(Path.Combine(args[0], "subtraction.py"),PythonEmitter.Emit(graph),new UTF8Encoding(false));
        graph.Nodes[0].Operation="multiply";
        File.WriteAllText(Path.Combine(args[0], "multiplication.py"),PythonEmitter.Emit(graph),new UTF8Encoding(false));
        graph.Nodes[0].Operation=null;
        File.WriteAllText(Path.Combine(args[0], "blocked.py"),PythonEmitter.Emit(graph),new UTF8Encoding(false));
        Console.WriteLine("PASS: " + assertions + " assertions C#");
    }
}
