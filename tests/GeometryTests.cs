using System;using Rhino.Geometry;using RiRtoPY;
class GeometryTests
{
    static int Main(){try{int count=0;var p=GeometryCapture.Read(new Point3d(1.25,2,3));if(p==null||p.Status!="captured"||!p.Properties[0].Contains("1.25"))throw new Exception("Point summary");count++;
    var vector=GeometryCapture.Read(new Vector3d(3,4,0));if(vector.Status!="captured"||!vector.Properties[0].Contains("Length=5"))throw new Exception("Vector summary");count++;
    var line=GeometryCapture.Read(new Line(new Point3d(0,0,0),new Point3d(0,0,7)));if(line.Status!="captured"||!line.Properties[0].Contains("Length=7"))throw new Exception("Line summary");count++;
    if(GeometryCapture.Read("text")!=null)throw new Exception("Non geometry ignored");count++;
    Console.WriteLine("PASS: "+count+" assertions geometriques simples ; courbes/surfaces/Brep natifs a valider dans Rhino.");return 0;
    }catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
}
