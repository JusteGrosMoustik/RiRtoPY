using System;
using System.Linq;

namespace RiRtoPY
{
    // A bounded translation profile, not a general list/tree interpreter.
    public static class RoomAreaEmitter
    {
        static readonly string[] Types = {
            "RhinoInside.Revit.GH.Components.Topology.QueryRooms",
            "Grasshopper.Kernel.Parameters.Param_Surface",
            "SurfaceComponents.AnalysisComponents.Component_AreaProperties",
            "MathComponents.OperatorComponents.Component_MassAddition",
            "Grasshopper.Kernel.Special.GH_Panel"
        };
        static readonly string[] Guids = {
            "5ddcb816-61a3-480f-ac45-67f66beb2e78", "deaf8653-5528-4286-807c-3de8b8dad781",
            "86b28a7e-94d9-4791-8306-e13e10d5f8d5", "5b850221-b527-4bd6-8c62-e94168cd6efa",
            "59e0b89a-e487-49f8-bab8-b5bab16be14c"
        };
        static readonly string[] Assemblies = { "RhinoInside.Revit.GH", "Grasshopper", "SurfaceComponents", "MathComponents", "Grasshopper" };
        static Node[] Chain(Graph graph)
        { return Types.Select(t => graph.Nodes.Single(n => n.Type == t)).ToArray(); }

        public static bool Matches(Graph graph)
        {
            if (graph.Schema != "rir-to-python/2" || graph.Nodes.Count != 5 ||
                Types.Any(t => graph.Nodes.Count(n => n.Type == t) != 1) ||
                graph.Nodes.Select(n => n.Id).Distinct().Count() != 5) return false;
            var chain = Chain(graph);
            for (int i = 0; i < 5; i++)
            {
                var n = chain[i];
                if (n.Issues.Count != 0 || !String.Equals(n.ComponentGuid, Guids[i], StringComparison.OrdinalIgnoreCase) ||
                    n.Assembly == null || !n.Assembly.StartsWith(Assemblies[i] + ", Version=", StringComparison.Ordinal) ||
                    n.Outputs.Count != (i == 2 || i == 3 ? 2 : 1)) return false;
                if (n.Inputs.Any(p => p.Reverse || p.Simplify || p.Mapping != "None" || !String.IsNullOrEmpty(p.Expression))) return false;
                if (i == 0) continue;
                if (n.Inputs.Count != 1) return false;
                var input = n.Inputs[0];
                if (input.Access != (i == 3 ? "list" : "item") || input.Sources.Count != 1 ||
                    input.Sources[0].Node != chain[i-1].Id || input.Sources[0].Output != 0) return false;
            }
            // Pin the implicit phase/default semantics to the inspected RiR release.
            if (!chain[0].Assembly.StartsWith("RhinoInside.Revit.GH, Version=1.35.", StringComparison.Ordinal)) return false;
            var query = chain[0].Inputs;
            string[] names = { "Point", "Placed", "Number", "Name", "Level" };
            if (!query.Select(p => p.Name).SequenceEqual(names)) return false;
            return query.All(p => p.Access == "item" && p.Optional && p.Sources.Count == 0 &&
                (p.Name == "Placed" ? p.Literal == "True" && p.HasPersistentData : p.Literal == null && !p.HasPersistentData));
        }

        public static string Emit(Graph graph)
        {
            if (!Matches(graph)) throw new InvalidOperationException("Profil pieces/surfaces incompatible.");
            var chain = Chain(graph);
            string[] values = { "rooms", "surfaces", "{'Area': areas, 'Centroid': centroids}", "{'Result': total, 'Partial Results': partials}", "total" };
            string outputs = String.Join(",\n", chain.Select((n, i) => new { n, i }).Where(x => x.n.Selected)
                .Select(x => "        " + PythonEmitter.Quote(x.n.Id) + ": " + values[x.i]));
            return Template.Replace("__OUTPUTS__", outputs);
        }

        const string Template = @"# -*- coding: utf-8 -*-
# RiRtoPY: Query Rooms -> Surface -> Area -> Mass Addition -> terminal Panel.
# Experimental read-only profile. Run inside Rhino.Inside.Revit on its API thread.
# Requires RhinoInside.Revit.GH 1.35; no Grasshopper graph is executed.
# Areas are in squared Rhino model units. Panel returns its numeric display value.
# Missing/invalid geometry raises an error rather than silently changing the total.

def accumulate(values):
    total = 0.0
    partials = []
    for value in values:
        total += value
        partials.append(total)
    return total, partials


def run(document=None):
    import clr
    clr.AddReference('RevitAPI')
    clr.AddReference('RhinoCommon')
    clr.AddReference('RhinoInside.Revit')
    assembly = clr.AddReference('RhinoInside.Revit.GH')
    version = assembly.GetName().Version
    if (version.Major, version.Minor) != (1, 35):
        raise NotImplementedError('This profile requires RhinoInside.Revit.GH 1.35.')
    from Autodesk.Revit import DB
    from Rhino import RhinoDoc
    from Rhino.Geometry import AreaMassProperties
    from RhinoInside.Revit import Revit
    from RhinoInside.Revit.GH.Types import RoomElement

    document = Revit.ActiveDBDocument if document is None else document
    if document is None or not document.IsValidObject:
        raise ValueError('An active Revit document is required.')
    if RhinoDoc.ActiveDoc is None:
        raise ValueError('An active Rhino document is required for model units.')
    phases = list(document.Phases)
    if not phases:
        raise ValueError('The Revit document has no phase.')
    phase_id = phases[-1].Id
    rooms, surfaces, areas, centroids = [], [], [], []
    collector = DB.FilteredElementCollector(document)
    try:
        candidates = collector.OfCategory(DB.BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()
        for room in candidates:
            phase = room.get_Parameter(DB.BuiltInParameter.ROOM_PHASE)
            if phase is None or not phase.AsElementId().Equals(phase_id):
                continue
            if room.Location is None:
                continue
            # Same conversion used by RiR GraphicalElement -> GH_Surface.
            surface = RoomElement(room).TrimmedSurface
            if surface is None:
                raise ValueError('Room {0}: no trimmed surface.'.format(room.UniqueId))
            surfaces.append(surface)
            properties = AreaMassProperties.Compute(surface)
            if properties is None:
                raise ValueError('Room {0}: area computation failed.'.format(room.UniqueId))
            try:
                areas.append(properties.Area)
                centroids.append(properties.Centroid)
            finally:
                properties.Dispose()
            rooms.append(room)
    except:
        for surface in surfaces:
            surface.Dispose()
        raise
    finally:
        collector.Dispose()
    total, partials = accumulate(areas)
    # Caller owns returned Rhino Breps; dispose them when no longer needed.
    return {
        'total': total, 'areas': areas, 'centroids': centroids,
        'rooms': rooms, 'surfaces': surfaces, 'partials': partials,
        'unit': str(RhinoDoc.ActiveDoc.ModelUnitSystem) + '^2',
        'outputs': {
__OUTPUTS__
        }
    }
";
    }
}
