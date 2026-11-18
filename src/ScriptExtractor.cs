using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RiRtoPY
{
    // Only known read-only source properties; no editor, compiler or script execution.
    public static class ScriptExtractor
    {
        private static object Get(object value, string name)
        {
            if (value == null) return null;
            var property=value.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance);
            return property==null?null:property.GetValue(value,null);
        }
        private static object InterfaceGet(object value,string interfaceName,string name)
        {
            var type=value.GetType().GetInterfaces().FirstOrDefault(t=>t.FullName==interfaceName);
            var property=type==null?null:type.GetProperty(name);
            return property==null?null:property.GetValue(value,null);
        }
        private static void Part(ScriptSnapshot script,string name,object value)
        { if(value is string)script.Parts.Add(new ScriptPart{Name=name,Text=(string)value}); }
        public static ScriptSnapshot Read(object component,string typeName,Func<Dictionary<string,string>> archive)
        {
            bool legacy=typeName.StartsWith("ScriptComponents.",StringComparison.Ordinal);
            bool python=typeName.StartsWith("GhPython.",StringComparison.Ordinal);
            bool modern=typeName.StartsWith("RhinoCodePluginGH.Components.",StringComparison.Ordinal)&&component.GetType().GetInterfaces().Any(t=>t.FullName=="RhinoCodePlatform.GH.IScriptComponent");
            if(!legacy&&!python&&!modern&&!typeName.ToLowerInvariant().Contains("script"))return null;
            var result=new ScriptSnapshot{Status="unavailable",ComponentType=typeName,EngineVersion=component.GetType().Assembly.GetName().Version.ToString(),Adapter=legacy?"legacy-dotnet":python?"ghpython":modern?"rhino-code":"unsupported"};
            try
            {
                if(legacy)
                {
                    result.Language=Convert.ToString(Get(component,"Language"));
                    var source=Get(component,"ScriptSource");
                    if(source!=null)
                    {
                        Part(result,"imports",Get(source,"UsingCode"));Part(result,"script",Get(source,"ScriptCode"));Part(result,"additional",Get(source,"AdditionalCode"));
                        var refs=source.GetType().GetField("References");
                        var values=refs==null?null:refs.GetValue(source) as IEnumerable;
                        if(values!=null)foreach(var reference in values)result.References.Add(Convert.ToString(reference));
                    }
                    if(result.Parts.Count==0&&archive!=null)
                    {
                        var data=archive();
                        foreach(var key in new[]{"UsingSource","ScriptSource","AdditionalSource"})if(data.ContainsKey(key))Part(result,key,data[key]);
                        foreach(var key in data.Keys.Where(k=>k.StartsWith("Reference[",StringComparison.Ordinal)).OrderBy(k=>k))result.References.Add(data[key]);
                        result.Adapter="legacy-dotnet-archive";
                    }
                    if(String.IsNullOrEmpty(result.Language))result.Language=typeName.ToLowerInvariant().Contains("vb")?"VB.NET":"C#";
                }
                else if(python)
                {
                    result.Language="IronPython (legacy)";
                    if(Object.Equals(Get(component,"InputIsPath"),true)){result.Reason="Source referencee par chemin externe ; fichier non ouvert par l'exporteur.";return result;}
                    Part(result,"script",Get(component,"Code"));
                    // If code is supplied by a wire, the Code getter need not be the executed source.
                    if(Object.Equals(Get(component,"HiddenCodeInput"),false))
                    { result.Status="partial";result.Reason="Code fourni par une entree ; texte embarque ci-dessous, valeurs de l'entree a consulter. Aucun calcul relance."; }
                }
                else if(modern)
                {
                    result.Language=Convert.ToString(InterfaceGet(component,"RhinoCodePlatform.GH.IScriptComponent","LanguageSpec"));
                    if(Object.Equals(InterfaceGet(component,"RhinoCodePlatform.GH.Context.IScriptObject","HasExternScript"),true))
                    {result.Reason="Script externe reference : lecture du fichier externe desactivee.";return result;}
                    Part(result,"script",InterfaceGet(component,"RhinoCodePlatform.GH.IScriptComponent","Text"));
                    var libraries=InterfaceGet(component,"RhinoCodePlatform.GH.IScriptComponent","SharedLibraries") as IEnumerable;
                    if(libraries!=null)foreach(var library in libraries)result.References.Add(Convert.ToString(Get(library,"Name"))+" "+Convert.ToString(Get(library,"Uri")));
                    if(Object.Equals(Get(component,"UsingScriptInputParam"),true)){result.Status="partial";result.Reason="Script alimente par une entree ; les valeurs capturees peuvent etre perimees.";}
                }
                else {result.Reason="Aucun adaptateur de source connu pour ce type et cette version. Aucun code deduit des sorties.";return result;}
                if(result.Parts.Count>0&&result.Status!="partial")result.Status=result.Parts.All(p=>String.IsNullOrEmpty(p.Text))?"empty":"captured";
                if(result.Parts.Count==0&&String.IsNullOrEmpty(result.Reason))result.Reason="Proprietes de source absentes ou nulles pour l'adaptateur "+result.Adapter+" ("+typeName+").";
            }
            catch(Exception ex){result.Status="error";result.Reason=(ex.InnerException??ex).Message;}
            return result;
        }
    }
}
