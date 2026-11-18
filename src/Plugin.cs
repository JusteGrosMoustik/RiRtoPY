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
[assembly: AssemblyVersion("0.5.0.0")]
[assembly: AssemblyFileVersion("0.5.0.0")]

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
        private Graph graph;
        private string brief;

        public static void ShowWorkspace(bool checkUpdate = false)
        {
            if (instance == null || instance.IsDisposed) instance = new WorkspaceForm();
            instance.Show(); instance.BringToFront();
            if (checkUpdate) instance.update.PerformClick();
        }
        private WorkspaceForm()
        {
            Text = "RiRtoPY " + UpdateService.CurrentVersion + " - Extraire pour ChatGPT";
            Size = new Size(960,680); MinimumSize = new Size(720,480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(14) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.Controls.Add(new Label { Text = "Exporte le document complet : composants, groupes, clusters imbriques, connexions et valeurs.\nLecture seule, sans lancer le Solver. Les contenus inaccessibles sont signales.", AutoSize = true },0,0);
            var tools = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var analyze = new Button { Text = "Analyser le graphe", AutoSize = true };
            analyze.Click += (s,e) => Analyze(); tools.Controls.Add(analyze);
            export = new Button { Text = "Exporter pour ChatGPT", AutoSize = true, Enabled = false };
            export.Click += (s,e) => Export(); tools.Controls.Add(export);
            layout.Controls.Add(tools,0,1);
            preview = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas",10), BackColor = Color.FromArgb(25,32,41), ForeColor = Color.FromArgb(221,231,240), Text = "Le releve complet apparaitra ici apres analyse." };
            layout.Controls.Add(preview,0,2);
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
            layout.Controls.Add(updates,0,3);
            status = new Label { Text = "Pret. Aucune modification de la maquette Revit.", Dock = DockStyle.Fill, AutoEllipsis = true };
            layout.Controls.Add(status,0,4);
            Controls.Add(layout);
        }
        private void Analyze()
        {
            try
            {
                var doc = Instances.ActiveCanvas == null ? null : Instances.ActiveCanvas.Document;
                graph = GraphReader.Read(doc, false);
                brief = GraphReportEmitter.Emit(graph);
                preview.Text = brief.Replace("\r\n", "\n").Replace("\n", "\r\n");
                status.Text = GraphReportEmitter.AllNodes(graph).Count() + " objets (clusters inclus) | " + (graph.Complete ? "Releve disponible." : "Extraction incomplete : voir les limites du releve.");
                export.Enabled = true;
            }
            catch (Exception ex) { graph = null; brief = null; export.Enabled = false; status.Text = ex.Message; }
        }
        private void Export()
        {
            Analyze(); // Capture the active document again: never export a stale preview.
            if (graph == null) return;
            using (var dialog = new SaveFileDialog { Filter = "Dossier pour ChatGPT (*.md)|*.md", FileName = "grasshopper.analysis.md", OverwritePrompt = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string stem = Path.Combine(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName));
                    if (stem.EndsWith(".analysis", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 9);
                    string report = stem + ".graph.json";
                    if (File.Exists(report) && MessageBox.Show(this, "Remplacer aussi le JSON associe ?", "RiRtoPY", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                    using (var stream = File.Create(report))
                        GraphReportEmitter.WriteJson(stream, graph);
                    File.WriteAllText(dialog.FileName, brief, new UTF8Encoding(false));
                    status.Text = "Releve Markdown et JSON exportes. Transmettre le .md a ChatGPT pour analyser le fichier.";
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export impossible"); }
            }
        }
    }
}
