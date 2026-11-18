using System;
using System.Globalization;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace RiRtoPY
{
    public static class GeometryCapture
    {
        private static string N(double value){return value.ToString("R",CultureInfo.InvariantCulture);}
        private static string P(Point3d p){return "["+N(p.X)+", "+N(p.Y)+", "+N(p.Z)+"]";}
        private static string V(Vector3d v){return "["+N(v.X)+", "+N(v.Y)+", "+N(v.Z)+"]";}
        private static void Plane(GeometrySummary s,Plane p){s.Properties.Add("Plane origin="+P(p.Origin)+"; X="+V(p.XAxis)+"; Y="+V(p.YAxis)+"; Z="+V(p.ZAxis));}
        public static GeometrySummary Read(object value)
        {
            var goo=value as IGH_GeometricGoo;
            var wrapper=value as GH_ObjectWrapper;
            object geometry=wrapper==null?value:wrapper.Value;
            if(goo==null&&!(geometry is GeometryBase)&&!(geometry is Point3d)&&!(geometry is Vector3d)&&!(geometry is Plane)&&!(geometry is Arc)&&!(geometry is Circle)&&!(geometry is Line))return null;
            var summary=new GeometrySummary{Status="captured",Type=value.GetType().FullName};
            try
            {
                if(goo!=null)
                {
                    if(goo.IsReferencedGeometry)summary.Properties.Add("ReferenceID="+goo.ReferenceID);
                    if(!goo.IsGeometryLoaded){summary.Status="unavailable";summary.Reason="Geometrie non chargee ; reference non resolue par l'exporteur.";return summary;}
                    geometry=((IGH_Goo)goo).ScriptVariable();
                }
                var basis=geometry as GeometryBase;
                if(basis!=null)
                {
                    summary.Properties.Add("Valid="+basis.IsValid);
                    var box=basis.GetBoundingBox(true);
                    if(box.IsValid)summary.Properties.Add("BoundingBox world XYZ: min="+P(box.Min)+"; max="+P(box.Max));
                }
                if(geometry is Point3d)summary.Properties.Add("Point="+P((Point3d)geometry));
                else if(geometry is Vector3d)summary.Properties.Add("Vector="+V((Vector3d)geometry)+"; Length="+N(((Vector3d)geometry).Length));
                else if(geometry is Plane)Plane(summary,(Plane)geometry);
                else if(geometry is Circle){var c=(Circle)geometry;summary.Properties.Add("Radius="+N(c.Radius));Plane(summary,c.Plane);}
                else if(geometry is Arc){var a=(Arc)geometry;summary.Properties.Add("Radius="+N(a.Radius)+"; AngleRadians="+N(a.Angle)+"; Start="+P(a.StartPoint)+"; End="+P(a.EndPoint));Plane(summary,a.Plane);}
                else if(geometry is Line){var l=(Line)geometry;summary.Properties.Add("Start="+P(l.From)+"; End="+P(l.To)+"; Length="+N(l.Length));}
                var curve=geometry as Curve;
                if(curve!=null)
                {
                    summary.Properties.Add("Curve type="+curve.GetType().Name+"; Length="+N(curve.GetLength())+"; Domain=["+N(curve.Domain.T0)+","+N(curve.Domain.T1)+"]; Closed="+curve.IsClosed+"; Periodic="+curve.IsPeriodic);
                    summary.Properties.Add("Start="+P(curve.PointAtStart)+"; End="+P(curve.PointAtEnd)+"; TangentStart="+V(curve.TangentAtStart)+"; TangentEnd="+V(curve.TangentAtEnd));
                    Arc arc;if(curve.TryGetArc(out arc)){summary.Properties.Add("ArcRadius="+N(arc.Radius));Plane(summary,arc.Plane);}
                }
                var surface=geometry as Surface;
                if(surface!=null)
                {
                    var u=surface.Domain(0);var v=surface.Domain(1);double width,height;
                    summary.Properties.Add("Surface DomainU=["+N(u.T0)+","+N(u.T1)+"]; DomainV=["+N(v.T0)+","+N(v.T1)+"]");
                    if(surface.GetSurfaceSize(out width,out height))summary.Properties.Add("EstimatedSize U="+N(width)+"; V="+N(height));
                    summary.Properties.Add("Normal at UV midpoint="+V(surface.NormalAt(u.Mid,v.Mid)));
                    using(var area=AreaMassProperties.Compute(surface))if(area!=null)summary.Properties.Add("Area="+N(area.Area));
                }
                var brep=geometry as Brep;
                if(brep!=null)
                {
                    summary.Properties.Add("Brep Faces="+brep.Faces.Count+"; Edges="+brep.Edges.Count+"; Solid="+brep.IsSolid+"; Orientation="+brep.SolidOrientation);
                    if(brep.IsValid)
                    {
                        using(var area=AreaMassProperties.Compute(brep))if(area!=null)summary.Properties.Add("Area="+N(area.Area));
                        if(brep.IsSolid)using(var volume=VolumeMassProperties.Compute(brep))if(volume!=null)summary.Properties.Add("Volume="+N(volume.Volume));
                    }
                }
                var mesh=geometry as Mesh;
                if(mesh!=null)
                {
                    summary.Properties.Add("Mesh Vertices="+mesh.Vertices.Count+"; Faces="+mesh.Faces.Count+"; Closed="+mesh.IsClosed);
                    if(mesh.IsValid)using(var area=AreaMassProperties.Compute(mesh))if(area!=null)summary.Properties.Add("Area="+N(area.Area));
                }
                if(summary.Properties.Count==0){summary.Status="unavailable";summary.Reason="Type geometrique sans extracteur de proprietes connu.";}
            }
            catch(Exception ex){summary.Status="partial";summary.Reason=(ex.InnerException??ex).Message;}
            return summary;
        }
    }
}
