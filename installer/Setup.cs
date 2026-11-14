using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("RiRtoPY Setup")]
[assembly: AssemblyVersion("0.3.0.0")]
class Setup : Form
{
    private readonly Label message;
    private readonly Button install;
    private readonly string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper", "Libraries", "RiRtoPY");
    [STAThread] static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Setup());
    }
    Setup()
    {
        Text = "Installer RiRtoPY 0.3.0";
        ClientSize = new Size(620,300); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI",10);
        Controls.Add(new Label { Text = "RiRtoPY 0.3.0\nPlugin Grasshopper - prototype de conversion Python", Location = new Point(22,20), Size = new Size(570,55), Font = new Font("Segoe UI",12,FontStyle.Bold) });
        Controls.Add(new Label { Text = "Installation pour ton compte Windows, sans droits administrateur.\nRhino et Grasshopper doivent deja etre installes.\n\nDestination : " + directory, Location = new Point(22,85), Size = new Size(570,95) });
        message = new Label { Text = "Enregistre ton travail et ferme Rhino et Revit avant de continuer.", Location = new Point(22,185), Size = new Size(570,55) };
        Controls.Add(message);
        install = new Button { Text = "Installer / Mettre a jour", Location = new Point(22,250), Size = new Size(200,32) };
        install.Click += (s,e) => Install(); Controls.Add(install);
        var close = new Button { Text = "Fermer", Location = new Point(490,250), Size = new Size(100,32) };
        close.Click += (s,e) => Close(); Controls.Add(close);
    }
    void Install()
    {
        var hosts = Process.GetProcesses().Where(p => {
            try { return p.ProcessName.Equals("Rhino",StringComparison.OrdinalIgnoreCase) || p.ProcessName.Equals("RhinoWIP",StringComparison.OrdinalIgnoreCase) || p.ProcessName.Equals("Revit",StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }).ToList();
        if (hosts.Count > 0)
        {
            message.Text = "Ferme Rhino/Revit, puis clique a nouveau sur Installer.\nAucun processus ne sera ferme automatiquement.";
            foreach (var host in hosts) host.Dispose();
            return;
        }
        string staging = null;
        try
        {
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory,"RiRtoPY.gha");
            staging = Path.Combine(directory, "RiRtoPY-" + Guid.NewGuid().ToString("N") + ".tmp");
            using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("RiRtoPY.gha"))
            {
                if (payload == null) throw new InvalidOperationException("Plugin integre introuvable.");
                using (var file = File.Create(staging)) payload.CopyTo(file);
            }
            // Atomic replacement preserves the installed file if replacement fails.
            if (File.Exists(target)) File.Replace(staging,target,Path.Combine(directory,"RiRtoPY.previous.bak"));
            else File.Move(staging,target);
            File.WriteAllText(Path.Combine(directory,"VERSION"),"0.3.0");
            message.Text = "Installation terminee. Relance Rhino ou Rhino.Inside.Revit, puis Grasshopper.\nOuvre le menu RiRtoPY ou double-clique sur son composant.";
            install.Enabled = false;
        }
        catch (Exception ex) { message.Text = "Installation impossible : " + ex.Message; }
        finally { if (staging != null && File.Exists(staging)) File.Delete(staging); }
    }
}
