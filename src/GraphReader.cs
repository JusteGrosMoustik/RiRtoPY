using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RiRtoPY
{
    public static class GraphReader
    {
        // Exact CLR types, not component captions which users can rename.
        private static readonly Dictionary<string,string> Rules = new Dictionary<string,string>
        {
            {"MathComponents.OperatorComponents.Component_VariableAddition", "add"},
            {"MathComponents.OperatorComponents.Component_VariableSubtraction", "subtract"},
            {"MathComponents.OperatorComponents.Component_VariableMultiplication", "multiply"},
            {"MathComponents.OperatorComponents.Operator_Division", "divide"}
        };

        private static object Property(object o, string name)
        {
            var p = o.GetType().GetProperty(name);
            return p == null ? null : p.GetValue(o, null);
        }

        private static string PersistentScalar(IGH_Param p)
        {
            var tree = Property(p, "PersistentData") as IGH_Structure;
            if (tree == null || tree.PathCount != 1 || tree.DataCount != 1 || tree.get_Path(0).ToString() != "{0}") return null;
            var value = tree.AllData(true).Cast<IGH_Goo>().FirstOrDefault();
            return value == null ? null : PythonEmitter.ScalarLiteral(value.ScriptVariable());
        }

        private static IGH_DocumentObject Owner(IGH_Param p)
        {
            return p.Attributes.GetTopLevel.DocObject;
        }

        private static Port ReadPort(IGH_Param p)
        {
            var result = new Port {
                Optional = p.Optional,
                HasPersistentData = (Property(p, "PersistentData") as IGH_Structure) != null && ((IGH_Structure)Property(p, "PersistentData")).DataCount > 0,
                Name = p.Name, Access = p.Access.ToString().ToLowerInvariant(),
                Mapping = p.DataMapping.ToString(), Reverse = p.Reverse, Simplify = p.Simplify,
                Expression = Convert.ToString(Property(p, "Expression"), CultureInfo.InvariantCulture),
                Literal = p.SourceCount == 0 ? PersistentScalar(p) : null
            };
            foreach (var source in p.Sources)
            {
                var owner = Owner(source);
                var component = owner as IGH_Component;
                result.Sources.Add(new Wire { Node = owner.InstanceGuid.ToString(), Output = component == null ? 0 : component.Params.Output.IndexOf(source) });
            }
            return result;
        }

        public static Graph Read(GH_Document doc, bool selectionOnly)
        {
            if (doc == null) throw new InvalidOperationException("Ouvrir un document Grasshopper.");
            var roots = doc.Objects.Where(o => !(o is RiRComponent) && !(o is Grasshopper.Kernel.Special.GH_Group) && (!selectionOnly || o.Attributes.Selected)).ToList();
            if (roots.Count == 0) throw new InvalidOperationException("Selectionner les composants a convertir, puis relancer l'analyse.");
            var graph = new Graph { Document = doc.DisplayName, Host = Rhino.RhinoApp.Version.ToString() };
            var seen = new HashSet<Guid>();
            Action<IGH_DocumentObject> add = null;
            add = o =>
            {
                if (!seen.Add(o.InstanceGuid)) return;
                var node = new Node {
                    Id = o.InstanceGuid.ToString(), ComponentGuid = o.ComponentGuid.ToString(),
                    Name = o.Name, NickName = o.NickName, Type = o.GetType().FullName,
                    Assembly = o.GetType().Assembly.GetName().FullName, Selected = roots.Contains(o)
                };
                graph.Nodes.Add(node);
                var component = o as IGH_Component;
                var parameter = o as IGH_Param;
                if (component != null)
                {
                    node.Inputs.AddRange(component.Params.Input.Select(ReadPort));
                    node.Outputs.AddRange(component.Params.Output.Select(p => p.Name));
                    if (component.Params.Input.Any(p => p.Locked) || component.Params.Output.Any(p => p.Locked || p.Reverse || p.Simplify || p.DataMapping != GH_DataMapping.None))
                        node.Issues.Add("Parametre desactive ou modificateur de sortie non traduit.");
                    string operation;
                    if (Rules.TryGetValue(node.Type, out operation) && o.GetType().Assembly.GetName().Name == "MathComponents") node.Operation = operation;
                    if (component.Params.Input.Count < 2 && node.Operation != null) node.Issues.Add("Nombre d'entrees arithmetiques inattendu.");
                    if (component.Hidden) { /* Preview visibility does not change computation. */ }
                    foreach (var input in component.Params.Input)
                        foreach (var source in input.Sources) add(Owner(source));
                }
                else if (parameter != null)
                {
                    node.Outputs.Add(parameter.Name);
                    if (node.Type == "Grasshopper.Kernel.Special.GH_NumberSlider")
                    {
                        node.Operation = "constant";
                        node.Literal = PythonEmitter.ScalarLiteral(Property(o, "CurrentValue"));
                    }
                    else if (node.Type == "Grasshopper.Kernel.Special.GH_BooleanToggle")
                    {
                        node.Operation = "constant";
                        node.Literal = PythonEmitter.ScalarLiteral(Property(o, "Value"));
                    }
                    else
                    {
                        // Typed parameters can coerce values; only generic pass-through is safe here.
                        if (node.Type == "Grasshopper.Kernel.Parameters.Param_GenericObject")
                        {
                            node.Operation = parameter.SourceCount == 0 ? "constant" : "identity";
                            if (parameter.SourceCount == 0) node.Literal = PersistentScalar(parameter);
                        }
                        if (parameter.SourceCount > 0) node.Inputs.Add(ReadPort(parameter));
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(Property(parameter, "Expression"), CultureInfo.InvariantCulture)))
                        node.Issues.Add("Expression du parametre non traduite.");
                    if (parameter.Reverse || parameter.Simplify || parameter.DataMapping != GH_DataMapping.None)
                        node.Issues.Add("Modificateur du parametre non traduit.");
                    foreach (var source in parameter.Sources) add(Owner(source));
                }
                else node.Issues.Add("Objet non pris en charge (cluster, script ou objet special).");
                var active = o as IGH_ActiveObject;
                if (active != null && active.Locked) node.Issues.Add("Objet desactive : comportement non traduit.");
            };
            foreach (var root in roots) add(root);
            return graph;
        }
    }
}
