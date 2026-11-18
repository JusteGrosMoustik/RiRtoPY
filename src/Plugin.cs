using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;

[assembly: AssemblyTitle("RiRtoPY")]
[assembly: AssemblyVersion("0.6.0.0")]
[assembly: AssemblyFileVersion("0.6.0.0")]

namespace RiRtoPY
{
    public sealed class AssemblyInfo : GH_AssemblyInfo
    {
        public override string Name { get { return "RiRtoPY"; } }
        public override string Description { get { return "Releve complet Grasshopper pour analyse avec ChatGPT"; } }
        public override Guid Id { get { return new Guid("c94b6862-104b-40a3-a7c4-0ab83504240b"); } }
        public override string AuthorName { get { return "Louhann Brocard"; } }
        public override string AuthorContact { get { return "https://github.com/JusteGrosMoustik/RiRtoPY"; } }
        public override Bitmap Icon { get { return PluginIcon.Create(); } }
    }
    internal static class PluginIcon
    {
        public static Bitmap Create()
        {
            var b = new Bitmap(24,24);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(29,42,56));
                using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
                    g.DrawString("PY", font, Brushes.Turquoise, 2,4);
            }
            return b;
        }
    }
    public sealed class Loading : GH_AssemblyPriority
    {
        private static Timer timer;
        public override GH_LoadingInstruction PriorityLoad()
        {
            timer = new Timer { Interval = 500 };
            timer.Tick += (s,e) => {
                var editor = Instances.DocumentEditor;
                if (editor == null || editor.MainMenuStrip == null) return;
                timer.Stop(); timer.Dispose();
                var menu = new ToolStripMenuItem("RiRtoPY");
                menu.DropDownItems.Add("Ouvrir RiRtoPY...", null, (a,b) => WorkspaceForm.ShowWorkspace());
                menu.DropDownItems.Add("Rechercher une mise a jour...", null, (a,b) => WorkspaceForm.ShowWorkspace(true));
                editor.MainMenuStrip.Items.Add(menu);
            };
            timer.Start();
            return GH_LoadingInstruction.Proceed;
        }
    }
    public sealed class RiRComponent : GH_Component
    {
        public RiRComponent() : base("RiRtoPY", "RiRtoPY", "Double-cliquer pour analyser, exporter et mettre a jour.", "RiRtoPY", "Outils") { }
        public override Guid ComponentGuid { get { return new Guid("ceca9abc-1669-47fc-9f6c-27c9e6068756"); } }
        protected override Bitmap Icon { get { return PluginIcon.Create(); } }
        public override void CreateAttributes() { m_attributes = new WorkspaceAttributes(this); }
        protected override void RegisterInputParams(GH_InputParamManager p) { }
        protected override void RegisterOutputParams(GH_OutputParamManager p) { }
        protected override void SolveInstance(IGH_DataAccess da) { Message = "v" + UpdateService.CurrentVersion; }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Ouvrir RiRtoPY...", (s,e) => WorkspaceForm.ShowWorkspace());
            Menu_AppendItem(menu, "Rechercher une mise a jour...", (s,e) => WorkspaceForm.ShowWorkspace(true));
        }
    }
    public sealed class WorkspaceAttributes : GH_ComponentAttributes
    {
        public WorkspaceAttributes(RiRComponent owner) : base(owner) { }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        { WorkspaceForm.ShowWorkspace(); return GH_ObjectResponse.Handled; }
    }
    public sealed class WorkspaceForm : Form
    {
        private static WorkspaceForm instance;
        private readonly TextBox preview;
        private readonly Label status;
        private readonly Button update;
        private readonly Button export;
        private readonly ComboBox level;
        private readonly ComboBox target;
        private readonly CheckBox hidePaths;
        private readonly CheckBox excludeValues;
        private readonly CheckBox excludeScripts;
        private Graph graph;
        private string brief;
        private Graph prepared;
        private bool changingTargets;
        private sealed class OutputChoice
        {
            public string Node; public int Port; public string Label;
            public override string ToString(){return Label;}
        }

        public static void ShowWorkspace(bool checkUpdate = false)
        {
            if (instance == null || instance.IsDisposed) instance = new WorkspaceForm();
            instance.Show(); instance.BringToFront();
            if (checkUpdate) instance.update.PerformClick();
        }
        private WorkspaceForm()
        {
            Text = "RiRtoPY " + UpdateService.CurrentVersion + " - Extraire pour ChatGPT";
            Size = new Size(1080,800); MinimumSize = new Size(800,600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, Padding = new Padding(14) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 135));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.Controls.Add(new Label { Text = "Exporte le document complet : composants, groupes, clusters imbriques, connexions et valeurs.\nLecture seule, sans lancer le Solver. Les contenus inaccessibles sont signales.", AutoSize = true },0,0);
            var tools = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var analyze = new Button { Text = "Analyser le graphe", AutoSize = true };
            analyze.Click += (s,e) => Analyze(); tools.Controls.Add(analyze);
            export = new Button { Text = "Exporter pour ChatGPT", AutoSize = true, Enabled = false };
            export.Click += (s,e) => Export(); tools.Controls.Add(export);
            var compare=new Button { Text="Comparer deux captures JSON",AutoSize=true };
            compare.Click+=(s,e)=>Compare();tools.Controls.Add(compare);
            layout.Controls.Add(tools,0,1);
            var options=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true};
            options.Controls.Add(new Label{Text="Rapport",AutoSize=true,Margin=new Padding(3,8,3,3)});
            level=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=235};
            level.Items.AddRange(new object[]{"Vue d'ensemble","Analyse fonctionnelle","Exhaustif"});level.SelectedIndex=1;
            options.Controls.Add(level);
            options.Controls.Add(new Label{Text="Perimetre",AutoSize=true,Margin=new Padding(12,8,3,3)});
            target=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=400};
            target.Items.Add(new OutputChoice{Label="Tout le document"});target.SelectedIndex=0;options.Controls.Add(target);options.SetFlowBreak(target,true);
            hidePaths=new CheckBox{Text="Masquer les chemins",Checked=true,AutoSize=true};
            excludeValues=new CheckBox{Text="Exclure les valeurs et expressions",AutoSize=true};
            excludeScripts=new CheckBox{Text="Exclure les codes sources",AutoSize=true};
            options.Controls.Add(hidePaths);options.Controls.Add(excludeValues);options.Controls.Add(excludeScripts);options.SetFlowBreak(excludeScripts,true);
            options.Controls.Add(new Label{Text="Perimetre cible : remontee conservative des dependances d'une sortie. Les noms et annotations restent visibles.\nExport local uniquement : relire l'apercu et les filtres avant de partager les fichiers.",AutoSize=true});
            layout.Controls.Add(options,0,2);
            level.SelectedIndexChanged+=(s,e)=>RefreshPreview();target.SelectedIndexChanged+=(s,e)=>RefreshPreview();
            hidePaths.CheckedChanged+=(s,e)=>RefreshPreview();excludeValues.CheckedChanged+=(s,e)=>RefreshPreview();excludeScripts.CheckedChanged+=(s,e)=>RefreshPreview();
            preview = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas",10), BackColor = Color.FromArgb(25,32,41), ForeColor = Color.FromArgb(221,231,240), Text = "Le releve complet apparaitra ici apres analyse." };
            layout.Controls.Add(preview,0,3);
            var updates = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0,8,0,0) };
            update = new Button { Text = "Rechercher une mise a jour", AutoSize = true };
            update.Click += async (s,e) => {
                update.Enabled = false;
                try { await UpdateService.CheckAsync(this, text => { if (!IsDisposed) status.Text = text; }); }
                finally { if (!IsDisposed) update.Enabled = true; }
            };
            updates.Controls.Add(update);
            var releases = new Button { Text = "Releases GitHub", AutoSize = true };
            releases.Click += (s,e) => { try { UpdateService.OpenReleases(); } catch (Exception ex) { status.Text = ex.Message; } };
            updates.Controls.Add(releases);
            layout.Controls.Add(updates,0,4);
            status = new Label { Text = "Pret. Aucune modification de la maquette Revit.", Dock = DockStyle.Fill, AutoEllipsis = true };
            layout.Controls.Add(status,0,5);
            Controls.Add(layout);
        }
        private void Analyze()
        {
            try
            {
                var doc = Instances.ActiveCanvas == null ? null : Instances.ActiveCanvas.Document;
                graph = GraphReader.Read(doc, false);
                var previous=target.SelectedItem as OutputChoice;
                changingTargets=true;target.BeginUpdate();target.Items.Clear();target.Items.Add(new OutputChoice{Label="Tout le document"});
                foreach(var node in GraphReportEmitter.AllNodes(graph))for(int i=0;i<node.Outputs.Count;i++)
                    target.Items.Add(new OutputChoice{Node=node.Id,Port=i,Label=(node.NickName??node.Name)+" / "+node.Outputs[i]+" ["+i+"] / "+node.Id});
                target.SelectedIndex=0;
                if(previous!=null&&previous.Node!=null)foreach(OutputChoice choice in target.Items)if(choice.Node==previous.Node&&choice.Port==previous.Port){target.SelectedItem=choice;break;}
                target.EndUpdate();changingTargets=false;RefreshPreview();
                if(prepared==null)return;
                status.Text = GraphReportEmitter.AllNodes(graph).Count() + " objets (clusters inclus) | " + (graph.Complete ? "Releve disponible." : "Extraction incomplete : voir les limites du releve.");
                export.Enabled = true;
            }
            catch (Exception ex) { changingTargets=false;graph = null; prepared=null;brief = null; export.Enabled = false; status.Text = ex.Message; }
        }
        private void RefreshPreview()
        {
            if(graph==null||preview==null||changingTargets)return;
            try
            {
                var scope=graph;var output=target.SelectedItem as OutputChoice;
                if(output!=null&&output.Node!=null)scope=GraphAnalysis.Project(graph,GraphAnalysis.Dependencies(graph,output.Node,output.Port));
                prepared=ExportPrivacy.Apply(scope,new PrivacyOptions{HidePaths=hidePaths.Checked,ExcludeValues=excludeValues.Checked,ExcludeScripts=excludeScripts.Checked});
                brief=CompactReport.Emit(prepared,(ReportLevel)Math.Max(0,level.SelectedIndex));
                preview.Text=brief.Replace("\r\n","\n").Replace("\n","\r\n");export.Enabled=true;
            }
            catch(Exception ex){prepared=null;brief=null;export.Enabled=false;if(status!=null)status.Text=ex.Message;}
        }
        private void Compare()
        {
            try
            {
                Graph before,after;
                using(var dialog=new OpenFileDialog{Filter="Capture Grasshopper (*.json)|*.json",Title="Choisir la capture AVANT"})
                {if(dialog.ShowDialog(this)!=DialogResult.OK)return;using(var stream=File.OpenRead(dialog.FileName))before=GraphAnalysis.ReadJson(stream);}
                using(var dialog=new OpenFileDialog{Filter="Capture Grasshopper (*.json)|*.json",Title="Choisir la capture APRES"})
                {if(dialog.ShowDialog(this)!=DialogResult.OK)return;using(var stream=File.OpenRead(dialog.FileName))after=GraphAnalysis.ReadJson(stream);}
                string result=GraphAnalysis.Compare(before,after);if(hidePaths.Checked)result=ExportPrivacy.Mask(result);
                preview.Text=result.Replace("\r\n","\n").Replace("\n","\r\n");
                using(var save=new SaveFileDialog{Filter="Comparaison Markdown (*.md)|*.md",FileName="grasshopper.comparison.md"})
                    if(save.ShowDialog(this)==DialogResult.OK)File.WriteAllText(save.FileName,result,new UTF8Encoding(false));
                status.Text="Comparaison terminee. Aucune definition executee.";
            }
            catch(Exception ex){status.Text=ex.Message;}
        }
        private void Export()
        {
            Analyze(); // Capture the active document again: never export a stale preview.
            if (prepared == null || brief == null) return;
            if(MessageBox.Show(this,"Exporter localement le rapport affiche, la vue d'ensemble et le JSON avec les filtres choisis ?\nLes textes, noms, annotations et codes conserves peuvent contenir des informations de projet. Relire avant partage.","Verifier l'export",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
            using (var dialog = new SaveFileDialog { Filter = "Dossier pour ChatGPT (*.md)|*.md", FileName = "grasshopper.analysis.md", OverwritePrompt = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string stem = Path.Combine(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName));
                    if (stem.EndsWith(".analysis", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 9);
                    string report = stem + ".graph.json",overview=stem+".overview.md";
                    if ((File.Exists(report)||File.Exists(overview)) && MessageBox.Show(this, "Remplacer aussi le JSON et la vue d'ensemble associes ?", "RiRtoPY", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                    using (var stream = File.Create(report))
                        GraphReportEmitter.WriteJson(stream, prepared);
                    File.WriteAllText(overview,CompactReport.Emit(prepared,ReportLevel.Overview),new UTF8Encoding(false));
                    File.WriteAllText(dialog.FileName, brief, new UTF8Encoding(false));
                    status.Text = "Rapport, vue d'ensemble et JSON exportes localement. Relire avant partage.";
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export impossible"); }
            }
        }
    }
}
