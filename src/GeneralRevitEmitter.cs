using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RiRtoPY
{
    public static class GeneralRevitEmitter
    {
        sealed class Rule
        {
            public string Type, Guid, Inputs, Required, Outputs, Expression;
            public Rule(string type, string guid, string inputs, string required, string outputs, string expression)
            { Type=type; Guid=guid; Inputs=inputs; Required=required; Outputs=outputs; Expression=expression; }
        }
        // Expressions are trusted templates. User names/IDs are always quoted.
        static readonly Rule[] Rules = {
            new Rule("Documents.ActiveDocument", "ee033516-c1dc-4c72-8fcd-f85f38a0f267", "", "", "Active Document", "{'Active Document': active_document()}"),
            new Rule("Elements.QueryElement", "bbcf5d5c-9abc-4e28-861a-584644edfc3d", "Document|Id", "Id", "Element", "{'Element': query_element($Id$, $Document$)}"),
            new Rule("Elements.QueryElements", "0f7da57e-6c05-4dd0-aabf-69e42df38859", "Document|Filter|Limit", "Filter", "Elements|Count", "query_elements($Filter$, $Document$, $Limit$)"),
            new Rule("Elements.ElementPropertyCategory", "5ac48de6-f706-4e88-a4ad-7a4439f1dab5", "Element", "Element", "Element|Category", "map_element(lambda e: {'Element': e, 'Category': element_category(e)}, $Element$, ['Element', 'Category'])"),
            new Rule("Elements.ElementPropertyName", "01934ad1-f31b-43e5-add9-c196f4a2467e", "Element|Name", "Element", "Element|Name", "map_element(lambda e: {'Element': e, 'Name': element_name(e)}, $Element$, ['Element', 'Name'])"),
            new Rule("Elements.ElementPin", "cc205221-1583-47d1-a715-226c39c3fb34", "Element|Pinned", "Element", "Element|Pinned", "map_element(lambda e: {'Element': e, 'Pinned': element_pinned(e)}, $Element$, ['Element', 'Pinned'])"),
            new Rule("Elements.ElementPropertyView", "440b6beb-cbbb-450a-ac32-0529e59e9e27", "Element", "Element", "Element|View Specific|View", "map_element(element_owner_view, $Element$, ['Element', 'View Specific', 'View'])"),
            new Rule("Filters.ElementCategoryFilter", "d08f7ab1-be36-45fa-b006-0078022db140", "Categories|Inverted", "Categories|Inverted", "Filter", "{'Filter': category_filter($Categories$, $Inverted$)}"),
            new Rule("Filters.ElementExcludeElementTypeFilter", "f69d485f-b262-4297-a496-93f5653f5d19", "Inverted", "Inverted", "Filter", "{'Filter': exclude_element_types_filter($Inverted$)}"),
            new Rule("Filters.ElementParameterFilter", "e6a1f501-bda4-4b78-8828-084b5eda926f", "Rules|Inverted", "Rules|Inverted", "Filter", "{'Filter': parameter_filter($Rules$, $Inverted$)}"),
            new Rule("Filters.ElementIntersectionFilter", "754c40d7-5ae8-4027-921c-0210bbdfab37", "Filters", "Filters", "Filter", "{'Filter': logical_and($Filters$)}"),
            new Rule("Filters.ElementUnionFilter", "61f75de1-ee65-4aa8-b9f8-40516be46c8d", "Filters", "Filters", "Filter", "{'Filter': logical_or($Filters$)}")
        };
        static string[] Parts(string text) { return text.Split(new [] {'|'}, StringSplitOptions.RemoveEmptyEntries); }
        static Rule Find(Node node)
        { return Rules.FirstOrDefault(r => node.Type == "RhinoInside.Revit.GH.Components." + r.Type && String.Equals(node.ComponentGuid, r.Guid, StringComparison.OrdinalIgnoreCase)); }
        static readonly string[] ExternalTypes = {
            "Grasshopper.Kernel.Parameters.Param_GenericObject", "Grasshopper.Kernel.Special.GH_Panel",
            "RhinoInside.Revit.GH.Parameters.Element", "RhinoInside.Revit.GH.Parameters.Category",
            "RhinoInside.Revit.GH.Parameters.ElementFilter", "RhinoInside.Revit.GH.Parameters.Input.BuiltInCategories"
        };
        static bool External(Node n) { return n.Inputs.Count == 0 && ExternalTypes.Contains(n.Type); }
        static bool Constant(Node n)
        { return n.Operation == "constant" && n.Inputs.Count == 0 && n.Literal != null && (n.Type == "Grasshopper.Kernel.Special.GH_NumberSlider" || n.Type == "Grasshopper.Kernel.Special.GH_BooleanToggle" || n.Type == "Grasshopper.Kernel.Parameters.Param_GenericObject"); }
        public static List<string> Problems(Graph graph)
        {
            if (!graph.Nodes.Any(n => Find(n) != null)) return null;
            var issues = new List<string>();
            if (graph.Schema != "rir-to-python/2") issues.Add("Reanalyser avec RiRtoPY 0.3.0 : metadonnees du rapport schema 2 requises.");
            if (graph.Nodes.Select(n => n.Id).Distinct().Count() != graph.Nodes.Count)
            { issues.Add("Identifiants de noeuds dupliques."); return issues; }
            foreach (var n in graph.Nodes)
            {
                issues.AddRange(n.Issues.Select(i => n.Name + " : " + i));
                var r = Find(n);
                if (r == null)
                {
                    if (!(Constant(n) || External(n))) issues.Add(n.Name + " : traduction generale indisponible pour " + n.Type);
                    if (n.Outputs.Count != 1) issues.Add(n.Name + " : source externe a plusieurs sorties non prise en charge.");
                    continue;
                }
                if (n.Assembly == null || !n.Assembly.StartsWith("RhinoInside.Revit.GH, Version=1.35.", StringComparison.Ordinal))
                    issues.Add(n.Name + " : profil general limite aux signatures RiR 1.35.");
                if (n.Outputs.Count == 0 || n.Outputs.Distinct().Count() != n.Outputs.Count || n.Outputs.Except(Parts(r.Outputs)).Any())
                    issues.Add(n.Name + " : signature des sorties non prise en charge.");
                if (n.Inputs.Select(p => p.Name).Distinct().Count() != n.Inputs.Count || n.Inputs.Select(p => p.Name).Except(Parts(r.Inputs)).Any() || Parts(r.Required).Except(n.Inputs.Select(p => p.Name)).Any())
                    issues.Add(n.Name + " : signature des entrees non prise en charge.");
                foreach (var p in n.Inputs)
                {
                    bool list = p.Name == "Categories" || p.Name == "Rules" || p.Name == "Filters";
                    if (p.Access != (list ? "list" : "item") || p.Reverse || p.Simplify || p.Mapping != "None" || !String.IsNullOrEmpty(p.Expression) || p.Sources.Count > 1)
                        issues.Add(n.Name + "/" + p.Name + " : arbre, modificateur ou sources multiples non traduits.");
                    if (p.Name == "Name" || p.Name == "Pinned")
                        if (p.Sources.Count != 0 || p.HasPersistentData || p.Literal != null || !p.Optional)
                            issues.Add(n.Name + "/" + p.Name + " : ecriture Revit non activee dans le convertisseur de graphe.");
                    if (p.Sources.Count == 0 && p.Literal == null && (p.HasPersistentData || !p.Optional))
                        issues.Add(n.Name + "/" + p.Name + " : valeur native requise ; connecter une source externe explicite.");
                    foreach (var wire in p.Sources)
                    {
                        var source = graph.Nodes.FirstOrDefault(s => s.Id == wire.Node);
                        if (source == null || wire.Output < 0 || wire.Output >= source.Outputs.Count)
                            issues.Add(n.Name + " : source ou indice de sortie invalide.");
                    }
                }
            }
            try { PythonEmitter.Ordered(graph); }
            catch (InvalidOperationException e) { issues.Add(e.Message); }
            return issues.Distinct().ToList();
        }
        public static string Emit(Graph graph)
        {
            var issues = Problems(graph);
            if (issues == null || issues.Count != 0) throw new InvalidOperationException("Graphe general incompatible.");
            var b = new StringBuilder();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RiRtoPY.rir_nodes.py"))
            using (var reader = new StreamReader(stream)) b.Append(reader.ReadToEnd());
            b.AppendLine("\n\ndef run(inputs=None, document=None):");
            b.AppendLine("    # External source values are required, never guessed from the report.");
            b.AppendLine("    inputs = {} if inputs is None else inputs");
            var ordered = PythonEmitter.Ordered(graph);
            var names = ordered.Select((n,i) => new { n.Id, Name="node_"+i }).ToDictionary(x => x.Id, x => x.Name);
            foreach (var n in ordered)
            {
                b.AppendLine("    # " + PythonEmitter.Quote(n.Name) + " | " + n.Id);
                var r = Find(n);
                string expression;
                if (r == null) expression = "{" + PythonEmitter.Quote(n.Outputs[0]) + ": " + (Constant(n) ? "inputs.get(" + PythonEmitter.Quote(n.Id) + ", " + n.Literal + ")" : "inputs[" + PythonEmitter.Quote(n.Id) + "]") + "}";
                else
                {
                    expression = r.Expression;
                    foreach (var key in Parts(r.Inputs))
                    {
                        var p = n.Inputs.FirstOrDefault(i => i.Name == key);
                        string value = "None";
                        if (p != null && p.Sources.Count == 1)
                        {
                            var w = p.Sources[0];
                            var source = graph.Nodes.First(s => s.Id == w.Node);
                            value = names[source.Id] + "[" + PythonEmitter.Quote(source.Outputs[w.Output]) + "]";
                        }
                        else if (p != null && p.Literal != null) value = p.Literal;
                        if (key == "Document" && value == "None") value = "document";
                        // Only element accessors map over a single flat element list.
                        if (key != "Element" && key != "Categories" && key != "Rules" && key != "Filters") value = "scalar_input(" + value + ")";
                        expression = expression.Replace("$" + key + "$", value);
                    }
                    expression = expression.Replace("$Document$", "document");
                }
                b.AppendLine("    " + names[n.Id] + " = " + expression);
            }
            b.AppendLine("    return {");
            foreach (var n in ordered.Where(n => n.Selected)) b.AppendLine("        " + PythonEmitter.Quote(n.Id) + ": " + names[n.Id] + ",");
            b.AppendLine("    }");
            return b.ToString();
        }
    }
}
