using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrokenHub
{
    class MainForm : Form
    {
        readonly Settings cfg;
        readonly BufferedPanel page = new BufferedPanel();
        readonly System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer();
        public bool ExitRequested;

        readonly List<Tool> tools = Catalog.All();
        readonly List<ToolCard> cards = new List<ToolCard>();
        volatile bool refreshing;
        DateTime lastRefresh = DateTime.MinValue;

        PictureBox logo;
        Label lblTitle, lblSub, secTools, lblFoot;
        Seg segLang;
        FlatButton btnAbout, btnRefresh, btnUpdateAll;
        Toggle tglPreviews;
        StatusBox info;

        public MainForm(Settings cfg)
        {
            this.cfg = cfg;
            Text = AppInfo.Name + " " + AppInfo.Version;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            AutoScaleMode = AutoScaleMode.None;
            Icon = Theme.AppIcon(32);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            using (var g = CreateGraphics()) Theme.S = Math.Max(1f, g.DpiX / 96f);

            page.Dock = DockStyle.Fill;
            page.BackColor = Theme.Bg;
            page.AutoScroll = true;
            page.Resize += delegate { DoLayout(); };
            Controls.Add(page);

            BuildUi();
            InitialSize();

            tick.Interval = 200;
            tick.Tick += delegate { OnTick(); };
            tick.Start();
        }

        static int P(float v) { return Theme.P(v); }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Theme.DarkTitle(this); }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshAll();
        }

        // Başka bir araçtan dönünce (ör. kaldırma sonrası) kurulu sürümler tazelenir; GitHub'a en fazla 5 dakikada bir sorulur
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (!AnyBusy && !refreshing && lastRefresh != DateTime.MinValue)
            {
                foreach (var t in tools) Catalog.ReadInstalled(t);
                UpdateUi();
            }
        }

        bool AnyBusy
        {
            get { foreach (var t in tools) if (t.Busy) return true; return false; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (AnyBusy && !ExitRequested)
            {
                e.Cancel = true;
                ConfirmDialog.Info(this, L.T("Kurulum sürüyor", "Setup in progress"), L.T("Kurulum bitince kapatabilirsin.", "You can close the app when the setup finishes."));
                return;
            }
            ExitRequested = true;
            SavePlacement();
            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        {
            if (key == Keys.F5) { RefreshAll(); return true; }
            return base.ProcessCmdKey(ref msg, key);
        }

        public void RequestExit()
        {
            if (AnyBusy) return;
            ExitRequested = true;
            Close();
        }

        public void BringToFront2()
        {
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Show();
            Activate();
            BringToFront();
        }

        void InitialSize()
        {
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            var frame = SizeFromClientSize(Size.Empty);
            MinimumSize = SizeFromClientSize(new Size(P(600), P(480)));
            ClientSize = new Size(Math.Min(P(980), wa.Width - frame.Width), Math.Min(P(720), wa.Height - frame.Height));
            try
            {
                var p = cfg.Window.Split(',');
                if (p.Length == 5)
                {
                    var r = new Rectangle(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                    bool visible = false;
                    foreach (var sc in Screen.AllScreens)
                        if (sc.WorkingArea.IntersectsWith(new Rectangle(r.X + 40, r.Y + 10, Math.Max(1, r.Width - 80), 40))) visible = true;
                    if (visible && r.Width >= MinimumSize.Width && r.Height >= MinimumSize.Height)
                    {
                        StartPosition = FormStartPosition.Manual;
                        Bounds = r;
                        if (p[4] == "1") WindowState = FormWindowState.Maximized;
                    }
                }
            }
            catch { }
        }

        void SavePlacement()
        {
            var r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            cfg.Window = r.X + "," + r.Y + "," + r.Width + "," + r.Height + "," + (WindowState == FormWindowState.Maximized ? "1" : "0");
            cfg.Save();
        }

        // ------------------------------------------------------------------ Arayüz
        T Add<T>(T c) where T : Control
        {
            if (c.Font == DefaultFont || !(c is Label)) c.Font = Font;
            page.Controls.Add(c);
            return c;
        }

        FlatButton Btn(string text, BtnKind kind, EventHandler click)
        {
            var b = Add(new FlatButton(text, kind));
            b.Click += click;
            return b;
        }

        void BuildUi()
        {
            SuspendLayout();
            Font = new Font("Segoe UI", 9.5f);

            logo = new PictureBox();
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.BackColor = Theme.Bg;
            logo.Image = Theme.Logo();
            page.Controls.Add(logo);

            lblTitle = new Label();
            lblTitle.Text = AppInfo.Name;
            lblTitle.Font = new Font(Font.FontFamily, 15f, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Text;
            lblTitle.AutoSize = true;
            page.Controls.Add(lblTitle);

            lblSub = new Label();
            lblSub.Text = L.T("Broken araçlarını tek yerden kur, güncelle, aç.", "Install, update and open the Broken tools from one place.");
            lblSub.ForeColor = Theme.Sub;
            lblSub.AutoSize = true;
            page.Controls.Add(lblSub);

            segLang = Add(new Seg());
            segLang.Items = new[] { "TR", "EN" };
            segLang.Font = new Font("Segoe UI", 8.5f);
            segLang.Selected = Math.Max(0, Array.IndexOf(L.Codes, L.Lang));
            segLang.Changed += delegate (int i)
            {
                if (AnyBusy || refreshing) { segLang.Selected = Array.IndexOf(L.Codes, L.Lang); return; }
                cfg.Lang = L.Codes[i];
                cfg.Save();
                L.Set(cfg.Lang);
                BeginInvoke((MethodInvoker)RebuildUi);
            };
            btnAbout = Btn(L.T("Hakkında", "About"), BtnKind.Ghost, delegate { using (var a = new AboutDialog()) a.ShowDialog(this); });

            info = Add(new StatusBox());

            btnRefresh = Btn(L.T("Yenile", "Refresh"), BtnKind.Secondary, delegate { RefreshAll(); });
            btnUpdateAll = Btn(L.T("Hepsini güncelle", "Update all"), BtnKind.Primary, delegate { UpdateAll(); });
            btnUpdateAll.Visible = false;
            tglPreviews = Add(new Toggle(L.T("Önizleme sürümleri", "Preview versions")));
            tglPreviews.Checked = cfg.Previews;
            tglPreviews.Changed += delegate (bool on)
            {
                cfg.Previews = on;
                cfg.Save();
                RefreshAll();
            };

            secTools = new Label();
            secTools.Text = L.Upper(L.T("Araçlar", "Tools"));
            secTools.ForeColor = Theme.Sub;
            secTools.BackColor = Theme.Bg;
            secTools.AutoSize = true;
            page.Controls.Add(secTools);
            secTools.Font = new Font(Font.FontFamily, 8f, FontStyle.Bold);

            cards.Clear();
            foreach (var t in tools)
            {
                var c = Add(new ToolCard(t, Font));
                var tt = t;
                c.Main.Click += delegate { MainAction(tt); };
                c.Open.Click += delegate { Open(tt); };
                c.Remove.Click += delegate { Remove(tt); };
                c.Web.Click += delegate { Browse(tt.PageUrl != null && tt.HasUpdate ? tt.PageUrl : tt.Repo); };
                cards.Add(c);
            }

            lblFoot = new Label();
            lblFoot.Text = L.T("Kurulum paketleri GitHub sürümlerinden indirilir ve SHA-256 özetleriyle doğrulanır. Broken Hub kendisi yönetici izni istemez; izin isteyen araçlar için Windows sorar.",
                               "Setups are downloaded from the GitHub releases and checked against their SHA-256 sums. Broken Hub itself doesn't need administrator rights; Windows asks for the tools that do.");
            lblFoot.ForeColor = Theme.Sub;
            lblFoot.BackColor = Theme.Bg;
            lblFoot.Font = new Font(Font.FontFamily, 8.5f);
            page.Controls.Add(lblFoot);

            UpdateUi();
            ResumeLayout();
            DoLayout();
        }

        void RebuildUi()
        {
            SuspendLayout();
            var old = new List<Control>();
            foreach (Control c in page.Controls) old.Add(c);
            page.Controls.Clear();
            foreach (var c in old) c.Dispose();
            BuildUi();
            ResumeLayout();
        }

        // Duyarlı düzen: içerik en fazla P(980) genişlikte ve ortada; pencere içerikten uzunsa dikeyde de ortalanır.
        void DoLayout()
        {
            if (lblFoot == null) return;
            int W = page.ClientSize.Width, H = page.ClientSize.Height;
            int pad = W < P(640) ? P(16) : P(28);
            int w = Math.Min(P(980), W - pad * 2);
            int x = (W - w) / 2;
            int height = Place(x, 0, w);
            bool fits = height + pad * 2 <= H;
            Place(x, fits ? (H - height) / 2 : pad - page.VerticalScroll.Value, w);
            page.AutoScrollMinSize = fits ? Size.Empty : new Size(0, height + pad * 2);
            page.Invalidate();
        }

        int Place(int x, int y0, int w)
        {
            int y = y0;
            bool narrow = w < P(600);
            logo.SetBounds(x, y, P(48), P(48));
            lblTitle.Location = new Point(x + P(62), y - P(1));
            lblSub.Location = new Point(x + P(64), y + P(30));
            int by = narrow ? y + P(62) : y + P(6);
            btnAbout.SetBounds(x + w - P(90), by, P(90), P(32));
            segLang.SetBounds(x + w - P(90) - P(8) - P(96), by, P(96), P(32));
            y = narrow ? by + P(48) : y + P(70);

            info.SetBounds(x, y, w, info.Measure(w));
            y = info.Bottom + P(12);
            btnRefresh.SetBounds(x, y, P(130), P(38));
            btnUpdateAll.SetBounds(x + P(140), y, P(200), P(38));
            if (narrow) { y += P(46); tglPreviews.SetBounds(x, y, P(230), P(30)); y += P(44); }
            else { tglPreviews.SetBounds(x + w - P(230), y + P(4), P(230), P(30)); y += P(56); }

            secTools.Location = new Point(x, y);
            y += P(24);
            foreach (var c in cards)
            {
                c.SetBounds(x, y, w, c.Measure(w));
                c.DoLayout();
                y = c.Bottom + P(12);
            }
            y += P(4);
            lblFoot.MaximumSize = new Size(w, 0);
            lblFoot.AutoSize = true;
            lblFoot.Location = new Point(x, y);
            return y + lblFoot.Height - y0;
        }


        // ------------------------------------------------------------------ Durum
        void UpdateUi()
        {
            int installed = 0, updates = 0, errors = 0;
            foreach (var t in tools)
            {
                if (t.Installed != null) installed++;
                if (t.HasUpdate) updates++;
                if (t.FetchError != null) errors++;
            }
            string s;
            if (refreshing) { s = L.T("GitHub'daki son sürümlere bakılıyor…", "Checking the latest versions on GitHub…"); info.Kind = StatusKind.Active; }
            else
            {
                s = string.Format(L.T("{0} araç · {1} kurulu", "{0} tools · {1} installed"), tools.Count, installed);
                if (updates > 0) s += string.Format(L.T(" · {0} güncelleme var", " · {0} update(s) available"), updates);
                else if (installed > 0 && errors == 0) s += L.T(" · hepsi güncel", " · all up to date");
                if (errors > 0) s += L.T("\nBazı araçların son sürümüne bakılamadı; internet bağlantını kontrol edip Yenile'ye bas.", "\nCouldn't check some tools' latest version; check your connection and press Refresh.");
                info.Kind = updates > 0 || errors > 0 ? StatusKind.Warn : StatusKind.Idle;
            }
            foreach (var t in tools) if (t.Busy) { s = t.Name + ": " + t.BusyText; info.Kind = StatusKind.Active; }
            if (info.Text != s) { info.Text = s; DoLayout(); }
            info.Invalidate();

            btnRefresh.Enabled = !refreshing && !AnyBusy;
            tglPreviews.Enabled = !refreshing && !AnyBusy;
            bool showAll = updates > 1;
            if (btnUpdateAll.Visible != showAll) btnUpdateAll.Visible = showAll;
            btnUpdateAll.Text = string.Format(L.T("Hepsini güncelle ({0})", "Update all ({0})"), updates);
            btnUpdateAll.Enabled = !AnyBusy && !refreshing;
            foreach (var c in cards) { c.Lock = refreshing || AnyBusy; c.Sync(); }
        }

        void OnTick()
        {
            double p = -1;
            bool busy = false;
            foreach (var t in tools) if (t.Busy) { busy = true; p = t.Progress; }
            if (busy)
            {
                WinShell.Progress(Handle, Math.Max(0, p), p < 0 ? WinShell.Indeterminate : WinShell.Normal);
                foreach (var c in cards) c.Invalidate();
                UpdateUi();
            }
        }

        // ------------------------------------------------------------------ İşler
        void RefreshAll()
        {
            if (refreshing || AnyBusy) return;
            refreshing = true;
            UpdateUi();
            bool previews = cfg.Previews;
            var t0 = new Thread(delegate ()
            {
                Parallel.ForEach(tools, t =>
                {
                    Catalog.ReadInstalled(t);
                    Catalog.FetchLatest(t, previews);
                });
                Ui(delegate { refreshing = false; lastRefresh = DateTime.Now; UpdateUi(); });
            });
            t0.IsBackground = true;
            t0.Start();
        }

        void Ui(MethodInvoker m)
        {
            try { if (!IsDisposed) BeginInvoke(m); } catch { }
        }

        void MainAction(Tool t)
        {
            if (t.Installed == null || t.HasUpdate) Install(t, null);
            else Open(t);
        }

        void Install(Tool t, Action done)
        {
            if (t.Busy || t.SetupUrl == null) { if (done != null) done(); return; }
            if (t.Running && t.HasUpdate)
            {
                if (!ConfirmDialog.Ask(this, t.Name, L.T("Araç şu an açık. Güncelleme onu kapatıp yeni sürümü kuracak. Devam edilsin mi?", "The tool is running. Updating will close it and install the new version. Continue?"),
                    null, L.T("Güncelle", "Update"), false)) { if (done != null) done(); return; }
            }
            t.Busy = true;
            t.Progress = 0;
            t.LastMessage = "";
            t.BusyText = L.T("İndiriliyor…", "Downloading…");
            UpdateUi();
            var th = new Thread(delegate ()
            {
                string msg = null;
                bool failed = false;
                try
                {
                    bool sumsMissing;
                    string file = Catalog.Download(t, delegate (double f)
                    {
                        t.Progress = f;
                        t.BusyText = L.T("İndiriliyor… ", "Downloading… ") + (int)(f * 100) + "%";
                    }, out sumsMissing);
                    bool go = true;
                    if (sumsMissing)
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            go = ConfirmDialog.Ask(this, L.T("Özet dosyası yok", "No checksum file"),
                                L.T("Bu sürümle SHA256SUMS.txt gelmemiş, indirilen dosya doğrulanamadı. Yine de kurulsun mu?",
                                    "This release has no SHA256SUMS.txt, so the download couldn't be verified. Install anyway?"),
                                null, L.T("Kur", "Install"), true);
                        });
                    }
                    if (!go) { try { System.IO.File.Delete(file); } catch { } msg = L.T("Kurulum iptal edildi.", "Setup cancelled."); }
                    else
                    {
                        t.Progress = -1;
                        t.BusyText = t.NeedsAdmin ? L.T("Kuruluyor… (Windows yönetici izni isteyebilir)", "Installing… (Windows may ask for administrator rights)") : L.T("Kuruluyor…", "Installing…");
                        int code;
                        if (!Catalog.RunSetup(file, out code)) msg = L.T("Yönetici izni verilmedi, kurulum yapılmadı.", "Administrator rights weren't given; nothing was installed.");
                        else
                        {
                            Catalog.ReadInstalled(t);
                            if (code == 0) msg = string.Format(L.T("{0} kuruldu.", "{0} installed."), t.Installed ?? t.Latest);
                            else { failed = true; msg = string.Format(L.T("Kurulum {0} koduyla bitti.", "Setup ended with code {0}."), code); }
                        }
                    }
                }
                catch (Exception ex) { failed = true; msg = ex.Message; }
                Ui(delegate
                {
                    t.Busy = false;
                    t.Progress = -1;
                    t.LastMessage = msg ?? "";
                    t.LastFailed = failed;
                    WinShell.Progress(Handle, 0, failed ? WinShell.Error : WinShell.NoProgress);
                    if (failed) WinShell.Flash(Handle);
                    UpdateUi();
                    if (done != null) done();
                });
            });
            th.IsBackground = true;
            th.Start();
        }

        void UpdateAll()
        {
            var queue = new Queue<Tool>();
            foreach (var t in tools) if (t.HasUpdate) queue.Enqueue(t);
            Action next = null;
            next = delegate { if (queue.Count > 0) Install(queue.Dequeue(), next); };
            next();
        }

        void Open(Tool t)
        {
            try { Catalog.Launch(t); t.LastMessage = ""; }
            catch (Exception ex)
            {
                t.LastMessage = L.T("Açılamadı: ", "Couldn't open: ") + ex.Message;
                t.LastFailed = true;
            }
            UpdateUi();
        }

        void Remove(Tool t)
        {
            if (t.Busy) return;
            t.Busy = true;
            t.Progress = -1;
            t.BusyText = L.T("Kaldırma aracı açık…", "Uninstaller is open…");
            UpdateUi();
            var th = new Thread(delegate ()
            {
                string msg = "";
                bool failed = false;
                try
                {
                    if (!Catalog.RunUninstall(t)) msg = L.T("Kaldırma iptal edildi.", "Uninstall cancelled.");
                    Catalog.ReadInstalled(t);
                    if (t.Installed == null) msg = L.T("Kaldırıldı.", "Removed.");
                }
                catch (Exception ex) { failed = true; msg = ex.Message; }
                Ui(delegate
                {
                    t.Busy = false;
                    t.LastMessage = msg;
                    t.LastFailed = failed;
                    WinShell.Progress(Handle, 0, WinShell.NoProgress);
                    UpdateUi();
                });
            });
            th.IsBackground = true;
            th.Start();
        }

        static void Browse(string url)
        {
            try { System.Diagnostics.Process.Start(url); } catch { }
        }
    }

    // ------------------------------------------------------------------ Araç kartı
    class ToolCard : Ctl
    {
        public readonly Tool Tool;
        public readonly FlatButton Main, Open, Remove, Web;
        public bool Lock;
        readonly Image logo;
        readonly Font bold, small, btnBold;

        public ToolCard(Tool t, Font f)
        {
            Tool = t;
            Font = f;
            Cursor = Cursors.Default;
            bold = new Font(f.FontFamily, 12f, FontStyle.Bold);
            small = new Font(f.FontFamily, 8.5f);
            btnBold = new Font(f, FontStyle.Bold);
            try
            {
                using (var s = typeof(ToolCard).Assembly.GetManifestResourceStream("BrokenHub.tool-" + t.Key + ".png"))
                    if (s != null) logo = new Bitmap(s);
            }
            catch { }
            Main = Child(new FlatButton("", BtnKind.Primary));
            Open = Child(new FlatButton(L.T("Aç", "Open"), BtnKind.Secondary));
            Remove = Child(new FlatButton(L.T("Kaldır", "Uninstall"), BtnKind.DangerGhost));
            Web = Child(new FlatButton("GitHub", BtnKind.Ghost));
        }

        FlatButton Child(FlatButton b)
        {
            b.Font = Font;
            b.BackColor = Theme.Card;
            Controls.Add(b);
            return b;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { bold.Dispose(); small.Dispose(); btnBold.Dispose(); if (logo != null) logo.Dispose(); }
            base.Dispose(disposing);
        }

        static int P(float v) { return Theme.P(v); }

        int ButtonsWidth
        {
            get
            {
                int w = 0;
                foreach (var b in new[] { Web, Remove, Open, Main }) if (b.Visible) w += b.Width + P(8);
                return w;
            }
        }

        public int Measure(int width)
        {
            int textW = Math.Max(P(200), width - P(108) - ButtonsWidth - P(16));
            using (var g = CreateGraphics())
            {
                var s = g.MeasureString(Tool.Desc, Font, textW);
                int h = P(16) + bold.Height + P(4) + (int)Math.Ceiling(s.Height) + P(8) + Font.Height + P(18);
                if (Tool.LastMessage != "" && !Tool.Busy) h += small.Height + P(4);
                return Math.Max(P(112), h);
            }
        }

        // Düğme metinleri ve görünürlüğü duruma göre
        public void Sync()
        {
            var t = Tool;
            bool installed = t.Installed != null;
            if (!installed) { Main.Text = L.T("Kur", "Install"); Main.Visible = t.SetupUrl != null; }
            else if (t.HasUpdate) { Main.Text = L.T("Güncelle", "Update") + " → " + t.Latest; Main.Visible = true; }
            else { Main.Text = L.T("Aç", "Open"); Main.Visible = true; }
            Open.Visible = installed && t.HasUpdate;
            Remove.Visible = installed && !string.IsNullOrEmpty(t.Uninstall);
            Main.Enabled = Open.Enabled = Remove.Enabled = !Lock;
            if (installed && !t.HasUpdate) Main.Enabled = !t.Busy && t.ExePath != null;
            Open.Enabled = Open.Enabled && t.ExePath != null;
            DoLayout();
            Invalidate();
        }

        public void DoLayout()
        {
            int h = P(38), y = (Height - h) / 2, x = Width - P(18);
            foreach (var b in new[] { Main, Open, Remove, Web })
            {
                if (!b.Visible) continue;
                int w;
                using (var g = CreateGraphics()) w = Math.Max(P(b == Main ? 120 : 84), (int)g.MeasureString(b.Text, b.Kind == BtnKind.Primary ? btnBold : Font).Width + P(32));
                x -= w;
                b.SetBounds(x, y, w, h);
                x -= P(8);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Theme.Prep(e, Theme.Bg);
            var t = Tool;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color edge = t.HasUpdate ? Theme.Mix(Theme.Border, Theme.Warn, 0.45f) : Theme.Border;
            Theme.Fill(g, r, P(12), Theme.Card);
            Theme.Stroke(g, r, P(12), edge, 1);

            int lx = P(20), ls = P(64);
            if (logo != null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, lx, (Height - ls) / 2, ls, ls);
            }

            int tx = P(108), textW = Math.Max(P(200), Width - tx - ButtonsWidth - P(16)), y = P(16);
            Theme.Draw(g, t.Name, bold, new RectangleF(tx, y, textW, bold.Height + P(4)), Theme.Text, StringAlignment.Near, StringAlignment.Near, false);
            y += bold.Height + P(4);
            var ds = g.MeasureString(t.Desc, Font, textW);
            Theme.Draw(g, t.Desc, Font, new RectangleF(tx, y, textW, ds.Height + P(2)), Theme.Sub, StringAlignment.Near, StringAlignment.Near, true);
            y += (int)Math.Ceiling(ds.Height) + P(8);

            // Durum satırı
            string st; Color sc;
            if (t.Busy) { st = t.BusyText; sc = Theme.Accent; }
            else if (t.Installed == null)
            {
                st = L.T("Kurulu değil", "Not installed");
                if (t.Latest != null) st += L.T(" · son sürüm ", " · latest ") + t.Latest + (t.LatestPre ? L.T(" (önizleme)", " (preview)") : "");
                sc = Theme.Sub;
            }
            else if (t.HasUpdate)
            {
                st = L.T("Kurulu ", "Installed ") + t.Installed + L.T(" · yeni sürüm ", " · new version ") + t.Latest + (t.LatestPre ? L.T(" (önizleme)", " (preview)") : "");
                sc = Theme.Warn;
            }
            else
            {
                st = L.T("Kurulu ", "Installed ") + t.Installed + (t.Latest != null ? L.T(" · güncel", " · up to date") : "");
                sc = Theme.Ok;
            }
            if (!t.Busy && t.Running) st += L.T(" · açık", " · running");
            if (!t.Busy && t.FetchError != null) st += " · " + t.FetchError;
            if (!t.Busy && t.NeedsAdmin) st += L.T(" · yönetici izni ister", " · needs administrator rights");
            float d = P(8), cy = y + Font.Height / 2f;
            using (var b = new SolidBrush(sc)) g.FillEllipse(b, tx, cy - d / 2, d, d);
            Theme.Draw(g, st, Font, new RectangleF(tx + P(16), y, textW - P(16), Font.Height + P(4)), Theme.Mix(sc, Theme.Text, 0.35f), StringAlignment.Near, StringAlignment.Near, false);
            y += Font.Height + P(4);

            if (!t.Busy && t.LastMessage != "")
                Theme.Draw(g, t.LastMessage, small, new RectangleF(tx, y, textW, small.Height + P(4)), t.LastFailed ? Theme.Danger : Theme.Ok, StringAlignment.Near, StringAlignment.Near, false);

            // İlerleme çubuğu
            if (t.Busy)
            {
                var bar = new RectangleF(P(12), Height - P(7), Width - P(24), P(3));
                Theme.Fill(g, bar, P(1.5f), Theme.Field);
                if (t.Progress >= 0)
                    Theme.Fill(g, new RectangleF(bar.X, bar.Y, Math.Max(P(3), bar.Width * (float)t.Progress), bar.Height), P(1.5f), Theme.Accent);
                else
                {
                    float seg = bar.Width * 0.25f, pos = (float)((Environment.TickCount % 1600) / 1600.0) * (bar.Width + seg) - seg;
                    float a = Math.Max(bar.X, bar.X + pos), b2 = Math.Min(bar.Right, bar.X + pos + seg);
                    if (b2 > a) Theme.Fill(g, new RectangleF(a, bar.Y, b2 - a, bar.Height), P(1.5f), Theme.Accent);
                }
            }
        }
    }
}
