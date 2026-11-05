using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace BrokenHub
{
    // Ailedeki bir araç: sabit bilgileri + bu bilgisayardaki ve GitHub'daki durumu
    class Tool
    {
        public string Key, Name, Repo, Exe, AppId, DescTr, DescEn;
        public bool NeedsAdmin;

        // Bu bilgisayarda
        public string Installed;          // kurulu sürüm, kurulu değilse null
        public string InstallDir, Uninstall;
        public bool Running;

        // GitHub'da
        public string Latest, SetupUrl, SetupName, SumsUrl, PageUrl;
        public bool LatestPre, Fetched;
        public string FetchError;

        // İş sürerken
        public volatile bool Busy;
        public volatile string BusyText = "";
        public double Progress = -1;      // 0..1, -1: belirsiz
        public string LastMessage = "";
        public bool LastFailed;

        public string Desc { get { return L.T(DescTr, DescEn); } }
        public string RepoName { get { return Repo.Substring(Repo.LastIndexOf('/') + 1); } }
        public bool HasUpdate { get { return Installed != null && Latest != null && Catalog.IsNewer(Latest, Installed); } }

        public string ExePath
        {
            get
            {
                if (InstallDir == null) return null;
                string p = Path.Combine(InstallDir, Exe);
                return File.Exists(p) ? p : null;
            }
        }
    }

    static class Catalog
    {
        const string Owner = "https://github.com/HErkanuzun/";

        public static List<Tool> All()
        {
            var l = new List<Tool>();
            l.Add(Make("screen", "Broken Screen Resizer", "BrokenScreenResizer", "{6E0B7C1D-4E52-4B2B-9F0A-3C8A51D7B2E4}", false,
                "Monitörün bozuk kısmını Windows'un dışında bırakır: masaüstünü sağlam alana sığdırır, pencereleri oraya yerleştirir.",
                "Keeps the broken part of a monitor out of Windows: fits the desktop into the healthy area and keeps windows there."));
            l.Add(Make("disk", "Broken Disk Resizer", "BrokenDiskResizer", "{C500871B-7DB3-47EF-9E79-5F8E29895BEC}", true,
                "Hard diskin bozuk sektörlerini bulur, çevresine pay bırakır ve diski sadece sağlam kısımlarda bölümler.",
                "Finds a hard disk's bad sectors, leaves a margin around them and partitions only the healthy parts."));
            l.Add(Make("space", "Broken Space Finder", "BrokenSpaceFinder", "{9F55E652-A631-470B-8E88-F40B25CCECA9}", false,
                "Diski neyin doldurduğunu bulur: büyük klasörler, şişen önbellekler, unutulan veriler. Sonra güvenle temizler.",
                "Finds what's filling your disk: big folders, bloated caches, forgotten data. Then cleans it up safely."));
            l.Add(Make("mic", "Broken Microphone", "BrokenMicrophone", "{D6B999DB-A641-4367-B9DC-C99509B6AC90}", true,
                "Telefonunu, tabletini ya da onlara bağlı kablosuz kulaklığı Wi-Fi üzerinden bilgisayarın mikrofonu yapar. Telefona uygulama gerekmez.",
                "Turns your phone, tablet or the wireless headset connected to them into the computer's microphone over Wi-Fi. No app needed on the phone."));
            return l;
        }

        static Tool Make(string key, string name, string repo, string appId, bool admin, string tr, string en)
        {
            var t = new Tool();
            t.Key = key; t.Name = name; t.Repo = Owner + repo; t.Exe = repo + ".exe"; t.AppId = appId;
            t.NeedsAdmin = admin; t.DescTr = tr; t.DescEn = en;
            return t;
        }

        // ------------------------------------------------------------------ Bu bilgisayar
        // Inno Setup kurulumları "Uygulamalar" kaydına {AppId}_is1 adıyla yazılır (yönetici kurulumu HKLM, kullanıcı kurulumu HKCU)
        public static void ReadInstalled(Tool t)
        {
            t.Installed = null; t.InstallDir = null; t.Uninstall = null;
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using (var b = RegistryKey.OpenBaseKey(hive, view))
                        using (var k = b.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + t.AppId + "_is1"))
                        {
                            if (k == null) continue;
                            t.Installed = (k.GetValue("DisplayVersion") as string) ?? "?";
                            t.InstallDir = (k.GetValue("InstallLocation") as string ?? "").TrimEnd('\\');
                            if (t.InstallDir == "") t.InstallDir = null;
                            t.Uninstall = k.GetValue("UninstallString") as string;
                            if (t.InstallDir == null)
                            {
                                string icon = (k.GetValue("DisplayIcon") as string ?? "").Trim('"');
                                int comma = icon.LastIndexOf(',');
                                if (comma > 2) icon = icon.Substring(0, comma);
                                try { if (icon != "") t.InstallDir = Path.GetDirectoryName(icon); } catch { }
                            }
                            t.Running = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(t.Exe)).Length > 0;
                            return;
                        }
                    }
                    catch { }
                }
            t.Running = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(t.Exe)).Length > 0;
        }

        // ------------------------------------------------------------------ GitHub
        static WebClientEx Web()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
            var w = new WebClientEx();
            w.Headers[HttpRequestHeader.UserAgent] = "BrokenHub/" + AppInfo.Version;
            w.Encoding = Encoding.UTF8;
            return w;
        }

        class WebClientEx : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var r = base.GetWebRequest(address);
                r.Timeout = 20000;
                return r;
            }
        }

        // Son sürümü bulur. previews=false ise önizleme (pre-release) sürümleri atlanır.
        public static void FetchLatest(Tool t, bool previews)
        {
            t.Fetched = false; t.FetchError = null; t.Latest = null; t.SetupUrl = null; t.SumsUrl = null;
            try
            {
                string api = t.Repo.Replace("https://github.com/", "https://api.github.com/repos/") + "/releases?per_page=15";
                string json;
                using (var w = Web())
                {
                    w.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    json = w.DownloadString(api);
                }
                var js = new JavaScriptSerializer();
                js.MaxJsonLength = 16 << 20;
                var list = js.DeserializeObject(json) as object[];
                if (list == null) throw new Exception("GitHub");
                Dictionary<string, object> best = null;
                string bestTag = null;
                foreach (var o in list)
                {
                    var r = o as Dictionary<string, object>;
                    if (r == null) continue;
                    if (true.Equals(r["draft"])) continue;
                    bool pre = true.Equals(r["prerelease"]);
                    if (pre && !previews) continue;
                    string tag = r["tag_name"] as string;
                    if (tag == null || FindAsset(r, "-Setup.exe") == null) continue;
                    if (bestTag == null || IsNewer(tag, bestTag)) { best = r; bestTag = tag; }
                }
                if (best == null) { t.FetchError = L.T("GitHub'da yayımlanmış sürüm yok", "No release on GitHub yet"); return; }
                var setup = FindAsset(best, "-Setup.exe");
                var sums = FindAsset(best, "SHA256SUMS.txt");
                t.Latest = Norm(bestTag);
                t.LatestPre = true.Equals(best["prerelease"]);
                t.SetupUrl = setup["browser_download_url"] as string;
                t.SetupName = setup["name"] as string;
                t.SumsUrl = sums != null ? sums["browser_download_url"] as string : null;
                t.PageUrl = best["html_url"] as string;
                t.Fetched = true;
            }
            catch (WebException ex)
            {
                var hr = ex.Response as HttpWebResponse;
                if (hr != null && (int)hr.StatusCode == 403) t.FetchError = L.T("GitHub şimdilik istek sınırına ulaştı, biraz sonra yenile", "GitHub rate limit reached, refresh a bit later");
                else if (hr != null && (int)hr.StatusCode == 404) t.FetchError = L.T("GitHub'da bulunamadı", "Not found on GitHub");
                else t.FetchError = L.T("GitHub'a bağlanılamadı", "Couldn't reach GitHub");
            }
            catch (Exception) { t.FetchError = L.T("GitHub yanıtı okunamadı", "Couldn't read GitHub's answer"); }
        }

        static Dictionary<string, object> FindAsset(Dictionary<string, object> release, string suffix)
        {
            var assets = release.ContainsKey("assets") ? release["assets"] as object[] : null;
            if (assets == null) return null;
            foreach (var a in assets)
            {
                var d = a as Dictionary<string, object>;
                string n = d != null ? d["name"] as string : null;
                if (n != null && n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return d;
            }
            return null;
        }

        // ------------------------------------------------------------------ İndir, doğrula, kur
        public static string TempFolder
        {
            get { return Path.Combine(Path.GetTempPath(), "BrokenHub"); }
        }

        // Kurulum paketini indirir; SHA256SUMS.txt'deki özetle karşılaştırır. Hata olursa açıklamasıyla istisna atar.
        // sumsMissing: sürümde özet dosyası yoksa true döner (indirilen dosya yine de verilir; karar kullanıcının)
        public static string Download(Tool t, Action<double> progress, out bool sumsMissing)
        {
            sumsMissing = false;
            Directory.CreateDirectory(TempFolder);
            string path = Path.Combine(TempFolder, t.SetupName);
            string expected = null;
            if (t.SumsUrl != null)
            {
                string sums;
                using (var w = Web()) sums = w.DownloadString(t.SumsUrl);
                foreach (var line in sums.Split('\n'))
                {
                    var parts = line.Trim().Split(new[] { ' ', '\t', '*' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[parts.Length - 1].Equals(t.SetupName, StringComparison.OrdinalIgnoreCase)) expected = parts[0].ToLowerInvariant();
                }
            }
            if (expected == null) sumsMissing = true;

            var req = (HttpWebRequest)WebRequest.Create(t.SetupUrl);
            req.UserAgent = "BrokenHub/" + AppInfo.Version;
            req.Timeout = 30000;
            req.ReadWriteTimeout = 30000;
            using (var resp = req.GetResponse())
            using (var src = resp.GetResponseStream())
            using (var dst = File.Create(path))
            using (var sha = SHA256.Create())
            {
                long total = resp.ContentLength, done = 0;
                var buf = new byte[1 << 16];
                int n;
                while ((n = src.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, n);
                    sha.TransformBlock(buf, 0, n, null, 0);
                    done += n;
                    if (total > 0 && progress != null) progress((double)done / total);
                }
                sha.TransformFinalBlock(buf, 0, 0);
                if (expected != null)
                {
                    var sb = new StringBuilder();
                    foreach (var b in sha.Hash) sb.Append(b.ToString("x2"));
                    if (sb.ToString() != expected)
                    {
                        dst.Close();
                        try { File.Delete(path); } catch { }
                        throw new Exception(L.T("İndirilen dosyanın SHA-256 özeti sürümdekiyle uyuşmuyor; dosya silindi. Bağlantını kontrol edip tekrar dene.",
                                                "The downloaded file's SHA-256 doesn't match the release; it was deleted. Check your connection and try again."));
                    }
                }
            }
            return path;
        }

        // Kurulumu sessiz çalıştırır (sadece ilerleme penceresi). Yönetici isteyen kurulumlarda Windows UAC sorar.
        // false: kullanıcı UAC'ı iptal etti
        public static bool RunSetup(string path, out int exitCode)
        {
            exitCode = -1;
            var psi = new ProcessStartInfo(path, "/SILENT /SUPPRESSMSGBOXES /NORESTART /SP-");
            psi.UseShellExecute = true;
            try
            {
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit();
                    exitCode = p.ExitCode;
                }
                return true;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) return false;   // UAC iptal
                throw;
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        // Aracın kendi kaldırma aracını açar (verilerin silinip silinmeyeceğini o sorar)
        public static bool RunUninstall(Tool t)
        {
            if (string.IsNullOrEmpty(t.Uninstall)) return false;
            string cmd = t.Uninstall.Trim(), file, args = "";
            if (cmd.StartsWith("\""))
            {
                int q = cmd.IndexOf('"', 1);
                file = cmd.Substring(1, q - 1);
                args = cmd.Substring(q + 1).Trim();
            }
            else
            {
                int sp = cmd.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase);
                file = sp > 0 ? cmd.Substring(0, sp + 4) : cmd;
                args = sp > 0 ? cmd.Substring(sp + 5) : "";
            }
            var psi = new ProcessStartInfo(file, args);
            psi.UseShellExecute = true;
            try
            {
                using (var p = Process.Start(psi)) p.WaitForExit();
                return true;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) return false;
                throw;
            }
        }

        public static void Launch(Tool t)
        {
            string exe = t.ExePath;
            if (exe == null) throw new FileNotFoundException(t.Exe);
            var psi = new ProcessStartInfo(exe);
            psi.UseShellExecute = true;
            psi.WorkingDirectory = t.InstallDir;
            try { Process.Start(psi); }
            catch (Win32Exception ex) { if (ex.NativeErrorCode != 1223) throw; }
        }

        // ------------------------------------------------------------------ Sürüm karşılaştırma
        // "v1.01" > "v1.0", "1.6" > "1.01" (her parça sayı olarak karşılaştırılır)
        public static bool IsNewer(string a, string b)
        {
            var x = Norm(a).Split('.');
            var y = Norm(b).Split('.');
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                int p = 0, q = 0;
                if (i < x.Length) int.TryParse(x[i], out p);
                if (i < y.Length) int.TryParse(y[i], out q);
                if (p != q) return p > q;
            }
            return false;
        }

        public static string Norm(string v)
        {
            v = (v ?? "").Trim().TrimStart('v', 'V');
            int i = 0;
            while (i < v.Length && (char.IsDigit(v[i]) || v[i] == '.')) i++;
            return v.Substring(0, i).Trim('.');
        }
    }
}
