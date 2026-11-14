using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RiRtoPY
{
    // Deliberately strict: unsupported semantics must never look like a valid conversion.
    public static class PythonEmitter
    {
        public static string Quote(string value)
        {
            var s = new StringBuilder("u\"");
            foreach (char c in value ?? "")
            {
                if (c == '\\') s.Append("\\\\");
                else if (c == '"') s.Append("\\\"");
                else if (c < 32 || c > 126) s.Append("\\u" + ((int)c).ToString("x4"));
                else s.Append(c);
            }
            return s.Append('"').ToString();
        }

        public static string ScalarLiteral(object value)
        {
            if (value is bool) return (bool)value ? "True" : "False";
            if (value is string) return Quote((string)value);
            if (value is int || value is long || value is short || value is byte || value is decimal)
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (value is double || value is float)
            {
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (Double.IsNaN(d) || Double.IsInfinity(d)) return null;
                return d.ToString("R", CultureInfo.InvariantCulture);
            }
            return null;
        }

        public static List<Node> Ordered(Graph graph)
        {
            var byId = graph.Nodes.ToDictionary(n => n.Id);
            var state = new Dictionary<string, int>();
            var ordered = new List<Node>();
            Action<Node> visit = null;
            visit = n =>
            {
                int s;
                if (state.TryGetValue(n.Id, out s))
                {
                    if (s == 1) throw new InvalidOperationException("Cycle detecte : " + n.Name);
                    return;
                }
                state[n.Id] = 1;
                foreach (var wire in n.Inputs.SelectMany(p => p.Sources))
                {
                    Node source;
                    if (!byId.TryGetValue(wire.Node, out source))
                        throw new InvalidOperationException("Source absente : " + wire.Node);
                    visit(source);
                }
                state[n.Id] = 2;
                ordered.Add(n);
            };
            foreach (var node in graph.Nodes) visit(node);
            return ordered;
        }

        public static List<string> Problems(Graph graph)
        {
            if (RoomAreaEmitter.Matches(graph)) return new List<string>();
            var generalIssues = GeneralRevitEmitter.Problems(graph);
            if (generalIssues != null) return generalIssues;
            var problems = new List<string>();
            foreach (var n in graph.Nodes)
            {
                foreach (string issue in n.Issues) problems.Add(n.Name + " [" + n.Id + "] : " + issue);
                if (n.Operation != null && !new [] { "constant", "identity", "add", "subtract", "multiply", "divide" }.Contains(n.Operation))
                    problems.Add(n.Name + " : operation inconnue");
                if ((n.Operation == "identity" && n.Inputs.Count != 1) ||
                    (n.Operation == "divide" && n.Inputs.Count != 2) ||
                    (new [] { "add", "subtract", "multiply" }.Contains(n.Operation) && n.Inputs.Count < 2))
                    problems.Add(n.Name + " : nombre d'entrees invalide");
                if (String.IsNullOrEmpty(n.Operation)) problems.Add(n.Name + " : composant non traduit (" + n.Type + ")");
                if (n.Operation == "constant" && n.Literal == null) problems.Add(n.Name + " : valeur non scalaire");
                if (n.Operation != "constant" && n.Outputs.Count != 1) problems.Add(n.Name + " : sorties multiples non prises en charge");
                foreach (var p in n.Inputs)
                {
                    if (p.Reverse || p.Simplify || p.Mapping != "None" || !String.IsNullOrEmpty(p.Expression))
                        problems.Add(n.Name + "/" + p.Name + " : modificateur de donnees non traduit");
                    if (p.Access != "item") problems.Add(n.Name + "/" + p.Name + " : acces liste/arbre non traduit");
                    if (p.Sources.Count > 1) problems.Add(n.Name + "/" + p.Name + " : sources multiples non traduites");
                    if (p.Sources.Count == 0 && p.Literal == null) problems.Add(n.Name + "/" + p.Name + " : entree sans valeur scalaire persistante");
                    if (p.Sources.Any(w => w.Output != 0)) problems.Add(n.Name + " : indice de sortie non pris en charge");
                }
            }
            try { Ordered(graph); } catch (InvalidOperationException e) { problems.Add(e.Message); }
            return problems.Distinct().ToList();
        }

        public static string Emit(Graph graph)
        {
            if (RoomAreaEmitter.Matches(graph)) return RoomAreaEmitter.Emit(graph);
            var generalIssues = GeneralRevitEmitter.Problems(graph);
            if (generalIssues != null && generalIssues.Count == 0) return GeneralRevitEmitter.Emit(graph);
            var problems = Problems(graph);
            var b = new StringBuilder();
            b.AppendLine("# -*- coding: utf-8 -*-");
            b.AppendLine("# RiRtoPY " + graph.Version + " | prototype scalaire");
            b.AppendLine("# Les cles d'entrees/sorties sont les InstanceGuid des objets Grasshopper.");
            b.AppendLine("# Aucun appel a Grasshopper ni modification Revit dans cet export.");
            b.AppendLine("def run(inputs=None):");
            if (problems.Count > 0)
            {
                b.AppendLine("    # Export bloque : consulter le rapport .json.");
                b.AppendLine("    raise NotImplementedError(" + Quote(String.Join("\n", problems)) + ")");
                return b.ToString();
            }
            b.AppendLine("    inputs = {} if inputs is None else inputs");
            var ordered = Ordered(graph);
            var names = ordered.Select((n,i) => new { n.Id, Value = "v" + i }).ToDictionary(x => x.Id, x => x.Value);
            foreach (var n in ordered)
            {
                b.AppendLine("    # " + Quote(n.NickName + " / " + n.Name) + " | " + n.Id);
                string value;
                if (n.Operation == "constant")
                    value = "inputs.get(" + Quote(n.Id) + ", " + n.Literal + ")";
                else
                {
                    var args = n.Inputs.Select(p => p.Sources.Count == 1 ? names[p.Sources[0].Node] : p.Literal).ToList();
                    if (n.Operation == "identity") value = args[0];
                    else
                    {
                        string op = n.Operation == "add" ? " + " : n.Operation == "subtract" ? " - " : n.Operation == "multiply" ? " * " : n.Operation == "divide" ? " / " : null;
                        if (op == null) throw new InvalidOperationException("Operation inconnue : " + n.Operation);
                        // GH arithmetic is numeric; force / to true division in IronPython 2 as well.
                        value = "(" + String.Join(op, args.Select(a => "float(" + a + ")")) + ")";
                    }
                }
                b.AppendLine("    " + names[n.Id] + " = " + value);
            }
            b.AppendLine("    return {");
            foreach (var n in ordered.Where(n => n.Selected)) b.AppendLine("        " + Quote(n.Id) + ": " + names[n.Id] + ",");
            b.AppendLine("    }");
            return b.ToString();
        }
    }
}
