using System;using System.Collections.Generic;using System.IO;using System.Linq;using GH_IO.Serialization;using RiRtoPY;
public class ArchiveLegacyFake{public string Language{get{return "VB";}}public object ScriptSource{get{return null;}}}
class ReferenceArchiveTests
{
    static int nodes,groups,annotations,scripts,clusters,opened,protectedClusters,wires,invalid,parts;
    static string S(GH_Chunk c,string key){return c.ItemExists(key)?c.GetString(key):null;}
    static IEnumerable<GH_Chunk> All(GH_Chunk c){yield return c;foreach(var child in c.Chunks)foreach(var nested in All((GH_Chunk)child))yield return nested;}
    static void Visit(GH_Chunk root)
    {
        var objects=All(root).Where(c=>c.Name=="Object"&&c.FindChunk("Container")!=null).ToList();
        var ids=new HashSet<Guid>();var sources=new List<Guid>();var sourceOwners=new Dictionary<Guid,string>();
        foreach(var obj in objects)
        {
            var container=(GH_Chunk)obj.FindChunk("Container");nodes++;
            var name=S(obj,"Name");if(name=="Group")groups++;if(name=="Scribble"&&container.ItemExists("Text"))annotations++;
            foreach(var chunk in All(container))
            {
                if(chunk.ItemExists("InstanceGuid"))ids.Add(chunk.GetGuid("InstanceGuid"));
                foreach(var item in chunk.Items.Where(i=>i.Name=="Source")){var id=(Guid)item.InternalData;sources.Add(id);sourceOwners[id]=(name??"?")+" / "+(S(container,"NickName")??"?")+" / chunk="+chunk.Name;}
            }
            if(container.ItemExists("ScriptSource"))
            {
                var data=new Dictionary<string,string>();foreach(var key in new[]{"UsingSource","ScriptSource","AdditionalSource"})if(container.ItemExists(key))data[key]=container.GetString(key);
                var script=ScriptExtractor.Read(new ArchiveLegacyFake(),"ScriptComponents.Legacy.ComponentLegacyVbScript",()=>data);
                if(script.Status!="captured"&&script.Status!="empty")throw new Exception("Echec extraction code archive : "+script.Reason);
                foreach(var entry in data)if(!script.Parts.Any(p=>p.Name==entry.Key&&p.Text==entry.Value))throw new Exception("Source modifiee a l'extraction.");
                scripts++;parts+=script.Parts.Count;
            }
            if(container.ItemExists("ClusterDocument")||container.ItemExists("EncryptedDocument"))
            {
                clusters++;
                if(container.ItemExists("ClusterDocument"))
                {var nested=new GH_LooseChunk("Document");nested.Deserialize_Binary(container.GetByteArray("ClusterDocument"));opened++;Visit(nested);}
                else protectedClusters++;
            }
        }
        wires+=sources.Count;invalid+=sources.Count(id=>!ids.Contains(id));foreach(var id in sources.Where(id=>!ids.Contains(id)))Console.WriteLine("Serialized missing reference: "+id+" -> "+sourceOwners[id]);
    }
    static int Main(string[] args)
    {
        try
        {
            var archive=new GH_Archive();if(!archive.ReadFromFile(args[0]))throw new Exception("Fichier illisible.");Visit(archive.GetRootNode);
            string summary=String.Format("ObjectsIncludingGroups={0}; Groups={1}; ScribblesWithText={2}; ScriptsWithExactSource={3}; SourceParts={4}; Clusters={5}; Opened={6}; Protected={7}; Wires={8}; MissingSourceGuids={9}",nodes,groups,annotations,scripts,parts,clusters,opened,protectedClusters,wires,invalid);
            Console.WriteLine(summary);if(args.Length>1)File.WriteAllText(args[1],summary);
            if(args.Length>2&&args[2]=="--rampe-baseline")
            {if(nodes-groups!=938||groups!=134||annotations!=157||scripts!=14||clusters!=15||opened!=11||protectedClusters!=4||wires-invalid!=926||invalid!=1)throw new Exception("Regression par rapport au fichier Rampe de reference.");Console.WriteLine("PASS: baseline Rampe ; 926 references resolues et 1 reference archive orpheline connue.");}
            else if(invalid>0)throw new Exception("References de source absentes dans l'archive.");return 0;
        }
        catch(Exception e){Console.Error.WriteLine("FAIL archive : "+e.Message);return 1;}
    }
}
