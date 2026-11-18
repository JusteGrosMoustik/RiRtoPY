using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RiRtoPY
{
    public sealed class PrivacyOptions
    {
        public bool HidePaths=true;
        public bool ExcludeValues;
        public bool ExcludeScripts;
    }
    public static class ExportPrivacy
    {
        // Conservative masking of path tails. This may mask the remainder of a code line.
        // It is not a guarantee of anonymity: names and free text can still identify a project.
        private static readonly Regex Paths=new Regex(@"(?i)(?:[a-z]:[\\/]|\\\\)[^\r\n""'<>|]*|(?<![\w:])/(?:home|Users|mnt|Volumes|tmp|var|srv|opt)/[^\r\n""'<>|]*",RegexOptions.Compiled);
        public static string Mask(string text){return text==null?null:Paths.Replace(text,"[CHEMIN MASQUE]");}
        private static void Strings(object obj,HashSet<object> seen)
        {
            if(obj==null||obj is string||obj.GetType().IsValueType||!seen.Add(obj))return;
            var list=obj as IList;
            if(list!=null){for(int i=0;i<list.Count;i++)if(list[i] is string)list[i]=Mask((string)list[i]);else Strings(list[i],seen);return;}
            foreach(var f in obj.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
                if(f.FieldType==typeof(string))f.SetValue(obj,Mask((string)f.GetValue(obj)));else Strings(f.GetValue(obj),seen);
        }
        public static Graph Apply(Graph source,PrivacyOptions options)
        {
            var result=GraphAnalysis.Clone(source);
            foreach(var n in GraphReportEmitter.AllNodes(result))
            {
                if(options.ExcludeValues)
                {
                    if(n.Script==null)n.SourceText=null;
                    n.Settings.Clear();
                    foreach(var p in n.Inputs.Concat(n.OutputPorts))
                    {p.Persistent=new DataSnapshot{Status="redacted"};p.Volatile=new DataSnapshot{Status="redacted"};p.Literal=null;p.Expression=null;}
                    n.Literal=null;
                }
                if(options.ExcludeScripts&&n.Script!=null)
                {n.SourceText=null;n.Script.Parts.Clear();n.Script.References.Clear();n.Script.Status="redacted";n.Script.Reason="Code exclu par l'utilisateur a l'export.";}
            }
            // Existing diagnostics can contain excerpts of removed values or source; recompute then mask.
            if(options.ExcludeValues||options.ExcludeScripts)
            {
                foreach(var doc in GraphAnalysis.Documents(result))
                {
                    doc.Issues=doc.Issues.Select(i=>"Detail masque par le filtre de confidentialite.").ToList();
                    foreach(var n in doc.Nodes)
                    {n.Issues=n.Issues.Select(i=>"Detail masque par le filtre de confidentialite.").ToList();n.RuntimeErrors=n.RuntimeErrors.Select(i=>"Message runtime masque.").ToList();n.RuntimeWarnings=n.RuntimeWarnings.Select(i=>"Message runtime masque.").ToList();}
                }
                GraphAnalysis.Audit(result);
            }
            if(options.HidePaths)Strings(result,new HashSet<object>());
            result.ExportNotes.Add("Confidentialite : chemins="+(options.HidePaths?"masques":"inclus")+"; valeurs="+(options.ExcludeValues?"exclues":"incluses")+"; scripts="+(options.ExcludeScripts?"exclus":"inclus")+". Les noms, annotations et textes libres ne sont pas anonymises automatiquement. Relire avant partage.");
            if(options.ExcludeValues)result.ExportNotes.Add("Valeurs et expressions exclues ; cette capture ne permet pas de comparer leurs changements.");
            return result;
        }
    }
}
