using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RiRtoPY
{
    [DataContract] internal sealed class Release
    {
        [DataMember(Name="tag_name")] public string Tag { get; set; }
        [DataMember(Name="assets")] public List<ReleaseAsset> Assets { get; set; }
    }
    [DataContract] internal sealed class ReleaseAsset
    {
        [DataMember(Name="name")] public string Name { get; set; }
        [DataMember(Name="browser_download_url")] public string Url { get; set; }
    }
    public static class UpdateService
    {
        public const string ReleasesUrl = "https://github.com/JusteGrosMoustik/RiRtoPY/releases";
        public const string CurrentVersion = "0.3.0";

        public static void OpenReleases()
        {
            Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });
        }

        public static bool IsNewer(string tag, string installed)
        {
            Version remote, local;
            return Version.TryParse((tag ?? "").TrimStart('v'), out remote) &&
                Version.TryParse(installed, out local) && remote > local;
        }

        private static void CheckAssetUrl(string url)
        {
            Uri u;
            if (!Uri.TryCreate(url, UriKind.Absolute, out u) || u.Scheme != "https" || u.Host != "github.com" ||
                !u.AbsolutePath.StartsWith("/JusteGrosMoustik/RiRtoPY/releases/download/", StringComparison.Ordinal))
                throw new InvalidOperationException("Adresse de telechargement inattendue.");
        }

        public static async Task CheckAsync(IWin32Window owner, Action<string> status)
        {
            try
            {
                status("Recherche d'une mise a jour...");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(3);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("RiRtoPY/" + CurrentVersion);
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                    Release release;
                    using (var response = await client.GetAsync("https://api.github.com/repos/JusteGrosMoustik/RiRtoPY/releases/latest"))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            status("Aucune version stable publiee, ou depot inaccessible.");
                            MessageBox.Show(owner, "Aucune release stable disponible. Les versions seront proposees ici apres publication sur GitHub.", "RiRtoPY");
                            return;
                        }
                        response.EnsureSuccessStatusCode();
                        using (var stream = await response.Content.ReadAsStreamAsync())
                            release = (Release)new DataContractJsonSerializer(typeof(Release)).ReadObject(stream);
                    }
                    if (!IsNewer(release.Tag, CurrentVersion))
                    {
                        status("Version " + CurrentVersion + " a jour.");
                        MessageBox.Show(owner, "La version " + CurrentVersion + " est a jour.", "RiRtoPY");
                        return;
                    }
                    string fileName = "RiRtoPY-Setup-" + release.Tag.TrimStart('v') + ".exe";
                    var setup = (release.Assets ?? new List<ReleaseAsset>()).SingleOrDefault(a => a.Name == fileName);
                    var checksum = (release.Assets ?? new List<ReleaseAsset>()).SingleOrDefault(a => a.Name == "SHA256SUMS.txt");
                    if (setup == null || checksum == null) throw new InvalidOperationException("Release sans installateur ou empreinte SHA-256.");
                    if (MessageBox.Show(owner, "Version " + release.Tag + " disponible. Telecharger et ouvrir l'installateur ?\n\nEnregistre ton travail. L'installation necessitera la fermeture de Rhino et de Revit.", "Mise a jour RiRtoPY", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                    { status("Mise a jour disponible : " + release.Tag); return; }
                    CheckAssetUrl(setup.Url);
                    CheckAssetUrl(checksum.Url);
                    status("Telechargement de " + release.Tag + "...");
                    string sums = await client.GetStringAsync(checksum.Url);
                    string line = sums.Split('\n').SingleOrDefault(l => l.TrimEnd('\r').EndsWith("  " + fileName, StringComparison.Ordinal));
                    if (line == null) throw new InvalidOperationException("Empreinte de l'installateur absente.");
                    string expected = line.Substring(0, 64);
                    byte[] bytes = await client.GetByteArrayAsync(setup.Url);
                    string actual;
                    using (var hash = SHA256.Create()) actual = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    if (!String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Empreinte SHA-256 incorrecte. Installation annulee.");
                    string folder = Path.Combine(Path.GetTempPath(), "RiRtoPY", Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(folder);
                    string path = Path.Combine(folder, fileName);
                    File.WriteAllBytes(path, bytes);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    status("Installateur ouvert. Fermer Rhino/Revit pour terminer, puis relancer.");
                }
            }
            catch (Exception ex)
            {
                status("Mise a jour impossible : " + ex.Message);
                MessageBox.Show(owner, ex.Message + "\n\nLe bouton Releases GitHub permet un telechargement manuel.", "RiRtoPY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
