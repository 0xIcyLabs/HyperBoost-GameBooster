using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HyperBoost
{
    internal sealed class ServiceEntry { public string Name { get; set; } public string DisplayName { get; set; } public string Status { get; set; } public bool Exists { get; set; } public bool Busy { get; set; } public bool Disabled { get; set; } }

    internal static class Program
    {
        [STAThread] private static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
    }

    internal sealed class MainForm : Form
    {
        private static readonly string[] Services = {
            "wuauserv","VmwareAutostartService","VMAuthdService","VMUSBArbService","VMnetDHCP","VMware NAT Service","dosvc","usosvc","DiagTrack","dmwappushservice","WerSvc","Spooler","PrintNotify","DeviceAssociationBrokerSvc","Fax","TermService","SessionEnv","UmRdpService","RemoteRegistry","RemoteAccess","RasAuto","WinRM","XboxGipSvc","XblAuthManager","XblGameSave","XboxNetApiSvc","icssvc","PhoneSvc","SmsRouter","lfsvc","SensorDataService","SensrSvc","SensorService","WbioSrvc","RetailDemo","SharedAccess","WpcMonSvc","stisvc","FrameServer","FrameServerMonitor","wisvc","WMPNetworkSvc","WFDSConMgrSvc","SCPolicySvc","SCardSvr","ScDeviceEnum","seclogon","TapiSrv","NetTcpPortSharing","Netlogon","WalletService","SDRSVC","VSS","MapsBroker","CDPSvc","SysMain","OneSyncSvc"
        };
        private readonly BindingList<ServiceEntry> entries = new BindingList<ServiceEntry>();
        private readonly BindingSource source = new BindingSource();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label product = new Label(), productB = new Label(), summary = new Label(), footer = new Label();
        private readonly Button refresh = new Button(), languageButton = new Button();
        private TileButton restoreButton, junkButton, tweaksButton, agentButton;
        private Panel optStrip;
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly System.Windows.Forms.Timer gameTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        private bool autoBoosted, lastInGame;
        private readonly OptimizationButton optimize = new OptimizationButton();
        private readonly OptimizationButton boostButton = new OptimizationButton();
        private readonly ToolTip toolTip = new ToolTip();
        private AppLanguage language = AppLanguage.English;
        private readonly Dictionary<string, bool> baselineRunning = new Dictionary<string, bool>();
        private Guid prevScheme, createdScheme;
        private int prevTimerResolution;
        private bool timerBoosted, boostBusy;
        private GpuBoost gpuState = GpuBoost.None;
        private RamMonitor ramMonitor;
        private readonly Label boostNotice = new Label { AutoSize = false, AutoEllipsis = true, Height = 20, Top = 131, TextAlign = ContentAlignment.MiddleCenter, Visible = false, BackColor = Color.FromArgb(5,11,17), ForeColor = Color.FromArgb(255,180,81), Font = new Font("Consolas", 8F, FontStyle.Bold) };
        private bool cleaningUp; private bool exitConfirmed;

        internal MainForm()
        {
            RecoveryState.RestoreIfInterrupted();
            Agent.Load();
            Text = Texts.Product + " — Game Booster"; Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(1000, 640); Size = new Size(1200, 800); BackColor = Ui.Page; Font = Ui.Body(9.5f); DoubleBuffered = true;
            // ---- App bar (brand lockup left, refresh + language right) ----
            var appBar = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Ui.Panel };
            appBar.Paint += PaintAppBar;
            product.Font = Ui.Body(21f, true); product.ForeColor = Ui.TextPrimary; product.AutoSize = true; product.Location = new Point(Ui.S6 + 8, 7); product.Text = "HYPER";
            productB.Font = Ui.Body(21f, true); productB.ForeColor = Ui.Accent; productB.AutoSize = true; productB.Location = new Point(product.Right + 2, 7); productB.Text = "BOOST";
            appBar.Controls.AddRange(new Control[] { product, productB });
            summary.ForeColor = Ui.TextBody; summary.Font = Ui.Body(9f); summary.AutoSize = false; summary.Width = 320; summary.TextAlign = ContentAlignment.MiddleRight;
            appBar.Controls.Add(summary);
            refresh.FlatStyle = FlatStyle.Flat; refresh.FlatAppearance.BorderColor = Ui.Divider; refresh.FlatAppearance.MouseOverBackColor = Ui.CardHover; refresh.BackColor = Color.Transparent; refresh.ForeColor = Ui.TextBody; refresh.Font = Ui.Body(9.5f); refresh.Size = new Size(84, 30);
            languageButton.FlatStyle = FlatStyle.Flat; languageButton.FlatAppearance.BorderColor = Ui.Divider; languageButton.FlatAppearance.MouseOverBackColor = Ui.CardHover; languageButton.BackColor = Color.Transparent; languageButton.ForeColor = Ui.TextBody; languageButton.Font = Ui.Body(9.5f); languageButton.Size = new Size(110, 30);
            foreach (var button in new[] { refresh, languageButton }) { button.Top = 16; button.Anchor = AnchorStyles.Top | AnchorStyles.Right; appBar.Controls.Add(button); }
            appBar.Resize += delegate { LayoutAppBar(appBar); }; LayoutAppBar(appBar);
            // ---- Action panel: hero column (OPTIMIZE + BOOST) left, utility tile grid right ----
            optStrip = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Ui.Page };
            boostButton.SmallMode = true;
            restoreButton = new TileButton("↺", Ui.Danger) { Text = Texts.T(language, "restore"), Enabled = false };
            junkButton = new TileButton("✕", Ui.Warn) { Text = Texts.T(language, "junk") };
            tweaksButton = new TileButton("◆", Ui.Accent) { Text = Texts.T(language, "tweaks") };
            agentButton = new TileButton("▶", Ui.Warn) { Text = Texts.T(language, "agent") };
            optStrip.Controls.AddRange(new Control[] { optimize, boostButton, restoreButton, junkButton, tweaksButton, agentButton, boostNotice });
            Action layoutStrip = delegate
            {
                int tileW = 160, tileH = 42, gap = Ui.S3;
                int gridW = tileW * 2 + gap;
                // Hero column on the left; the 2x2 tile grid sits directly beside it (clustered),
                // not pushed to the far edge of the window.
                int avail = optStrip.ClientSize.Width - Ui.S5 - gridW - Ui.S3 - Ui.S5;
                int heroW = Math.Max(300, Math.Min(440, avail));
                int gridX = Ui.S5 + heroW + Ui.S3;

                optimize.Size = new Size(heroW, 58);
                boostButton.Size = new Size(heroW, 40);
                optimize.Location = new Point(Ui.S5, 24);
                boostButton.Location = new Point(Ui.S5, 24 + 58 + gap);

                restoreButton.Size = new Size(tileW, tileH);
                junkButton.Size = new Size(tileW, tileH);
                tweaksButton.Size = new Size(tileW, tileH);
                agentButton.Size = new Size(tileW, tileH);
                int gridH = tileH * 2 + gap;
                int gridTop = (150 - gridH) / 2;
                restoreButton.Location = new Point(gridX, gridTop + tileH + gap);
                junkButton.Location = new Point(gridX + tileW + gap, gridTop);
                tweaksButton.Location = new Point(gridX, gridTop);
                agentButton.Location = new Point(gridX + tileW + gap, gridTop + tileH + gap);

                boostNotice.Size = new Size(heroW, 20);
                boostNotice.Location = new Point(Ui.S5, optStrip.ClientSize.Height - 26);
            };
            optStrip.Resize += delegate { layoutStrip(); }; layoutStrip();
            SetupGrid();
            var footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Ui.Panel };
            footer.Dock = DockStyle.Fill; footer.BackColor = Ui.Panel; footer.ForeColor = Ui.TextMuted; footer.TextAlign = ContentAlignment.MiddleLeft;
            var versionLabel = new Label { Dock = DockStyle.Right, AutoSize = false, Width = 260, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0,9,20,0), BackColor = Ui.Panel, ForeColor = Ui.TextMuted, Font = Ui.Mono(8.5f), Text = "HyperBoost v1.5.1 • 0xIcyLabs" };
            footerPanel.Controls.Add(footer); footerPanel.Controls.Add(versionLabel);
            Controls.Add(grid); Controls.Add(footerPanel); Controls.Add(optStrip); Controls.Add(appBar);

            // The label re-creation above drops the field references' parents if any;
            // re-add handled dialog openers and app-bar wiring happen below via existing handlers.
            refresh.Click += async delegate { await LoadAsync(); };
            optimize.Click += async delegate { await Optimize(); };
            boostButton.Click += async delegate { await ApplyBoost(); };
            restoreButton.Click += async delegate { await RestoreBoost(); };
            junkButton.Click += delegate { if (boostBusy || optimize.IsBusy || boostButton.IsBusy) return; using (var junk = new JunkForm(language)) junk.ShowDialog(this); };
            tweaksButton.Click += delegate { if (boostBusy || optimize.IsBusy || boostButton.IsBusy) return; using (var tweaks = new TweaksForm(language)) tweaks.ShowDialog(this); };
            agentButton.Click += delegate { if (boostBusy || optimize.IsBusy || boostButton.IsBusy) return; using (var agent = new AgentForm(language)) agent.ShowDialog(this); UpdateAutoTimer(); };
            FormClosing += CleanupBoost;
            var langMenu = new ContextMenuStrip();
            foreach (AppLanguage lang in Enum.GetValues(typeof(AppLanguage)))
            {
                var item = new ToolStripMenuItem(Texts.LanguageNames[(int)lang]) { Tag = lang, Font = new Font("Segoe UI", 9.5F) };
                item.Click += delegate { language = (AppLanguage)item.Tag; ApplyLanguage(); };
                langMenu.Items.Add(item);
            }
            languageButton.Click += delegate { foreach (ToolStripItem entry in langMenu.Items) ((ToolStripMenuItem)entry).Checked = (AppLanguage)entry.Tag == language; langMenu.Show(languageButton, new Point(languageButton.Width - langMenu.Width + 0, languageButton.Height + 2)); };
            tray.DoubleClick += delegate { Show(); WindowState = FormWindowState.Normal; Activate(); };
            BuildTrayMenu();
            gameTimer.Tick += GameTick;
            UpdateAutoTimer();
            RegisterHotKey(Handle, 1, 0x1 | 0x2, 0x42);
            FormClosed += delegate { gameTimer.Stop(); UnregisterHotKey(Handle, 1); tray.Visible = false; tray.Dispose(); };
            Shown += async delegate { ApplyLanguage(); await LoadAsync(); };
        }
        private static void SetupButton(Button b, int width, Color back)
        {
            b.Width=width; b.Height=35; b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderColor=Color.FromArgb(0,232,179); b.FlatAppearance.MouseOverBackColor=Color.FromArgb(20,92,94); b.BackColor=back; b.ForeColor=Color.FromArgb(230,255,250); b.Font=new Font("Segoe UI Semibold",8.5F);
        }
        private void LayoutAppBar(Panel h)
        {
            languageButton.Left = h.ClientSize.Width - languageButton.Width - Ui.S4;
            refresh.Left = languageButton.Left - refresh.Width - Ui.S2;
            summary.AutoSize = true; summary.AutoSize = false; summary.Width = 320; summary.Location = new Point(refresh.Left - summary.Width - Ui.S4, 15);
            summary.TextAlign = ContentAlignment.MiddleRight;
        }
        private static void PaintAppBar(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender; var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(0, panel.Height - 1, panel.Width, 1);
            using (var line = new SolidBrush(Ui.Divider)) g.FillRectangle(line, rect);
            // Logo bolt glyph (left of the wordmark): soft accent glow + solid bolt, fully centered vertically.
            float boltX = 34f, boltY = panel.Height / 2f;
            var bolt = new[] { new PointF(7f, -18f), new PointF(-7f, 4f), new PointF(-1f, 4f), new PointF(-8f, 18f), new PointF(7f, -4f), new PointF(1f, -4f) };
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(bolt);
                using (var mx = new Matrix()) { mx.Translate(boltX, boltY); mx.Scale(1.05f, 1.05f); path.Transform(mx); }
                using (var halo = new SolidBrush(Color.FromArgb(38, Ui.Accent))) { using (var haloPath = new GraphicsPath()) { haloPath.AddPolygon(bolt); using (var m2 = new Matrix()) { m2.Translate(boltX, boltY); m2.Scale(1.7f, 1.7f); haloPath.Transform(m2); } g.FillPath(halo, haloPath); } }
                using (var b = new SolidBrush(Ui.Accent)) g.FillPath(b, path);
            }
            // accent underline spans the full logo lockup
            using (var p = new Pen(Color.FromArgb(160, Ui.Accent), 1f)) g.DrawLine(p, Ui.S5, panel.Height - 1, 136, panel.Height - 1);
        }
        private void SetupGrid()
        {
            grid.Dock=DockStyle.Fill; grid.BackgroundColor=Ui.Page; grid.BorderStyle=BorderStyle.None; grid.GridColor=Ui.Divider; grid.AutoGenerateColumns=false; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; grid.AllowUserToResizeRows=false; grid.ReadOnly=true; grid.RowHeadersVisible=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect=false; grid.EnableHeadersVisualStyles=false; grid.ColumnHeadersHeight=40; grid.RowTemplate.Height=36;
            grid.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle { BackColor=Ui.Panel, ForeColor=Ui.TextBody, Font=Ui.Body(9f,true), Padding=new Padding(12,5,12,5), SelectionBackColor=Ui.Panel };
            grid.DefaultCellStyle=new DataGridViewCellStyle { BackColor=Ui.Page, ForeColor=Ui.TextBody, SelectionBackColor=Ui.CardHover, SelectionForeColor=Ui.TextPrimary, Padding=new Padding(12,5,12,5) };
            grid.AlternatingRowsDefaultCellStyle=new DataGridViewCellStyle { BackColor=Color.FromArgb(9,18,24), ForeColor=Ui.TextBody, SelectionBackColor=Ui.CardHover, SelectionForeColor=Ui.TextPrimary, Padding=new Padding(12,5,12,5) };
            grid.EnableHeadersVisualStyles=false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name="DisplayName",DataPropertyName="DisplayName",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,FillWeight=43 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Name",DataPropertyName="Name",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,FillWeight=30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Status",DataPropertyName="Status",Width=130 });
            grid.Columns.Add(new DataGridViewButtonColumn { Name="Start",UseColumnTextForButtonValue=true,Width=92,FlatStyle=FlatStyle.Flat });
            grid.Columns.Add(new DataGridViewButtonColumn { Name="Stop",UseColumnTextForButtonValue=true,Width=92,FlatStyle=FlatStyle.Flat });
            grid.CellContentClick += GridClick; grid.ColumnHeaderMouseClick += GridHeaderClick; grid.CellFormatting += GridFormat; grid.CellPainting += GridPaint;
            source.DataSource=entries; grid.DataSource=source;
        }
        private void ApplyLanguage()
        {
            bool rtl = language == AppLanguage.Arabic;
            RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
            grid.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
            refresh.Text=Texts.T(language,"refresh"); languageButton.Text=Texts.LanguageNames[(int)language]; footer.Text="   "+Texts.T(language,"footer");
            grid.Columns["DisplayName"].HeaderText=Texts.T(language,"service"); grid.Columns["Name"].HeaderText=Texts.T(language,"name"); grid.Columns["Status"].HeaderText=Texts.T(language,"status"); grid.Columns["Start"].HeaderText=Texts.T(language,"startAll"); grid.Columns["Stop"].HeaderText=Texts.T(language,"stopAll"); ((DataGridViewButtonColumn)grid.Columns["Start"]).Text=Texts.T(language,"start"); ((DataGridViewButtonColumn)grid.Columns["Stop"]).Text=Texts.T(language,"stop"); source.ResetBindings(false);
            if(entries.Count==0)summary.Text=Texts.T(language,"loading");else UpdateSummary();
            if(boostNotice.Visible)boostNotice.Text=Texts.T(language,"boostNotice");
            if(!optimize.IsBusy) optimize.SetLabels(Texts.T(language,"optimize"), Texts.T(language,"optimizing"));
            if(!boostButton.IsBusy) boostButton.SetLabels(Texts.T(language,"boost"), Texts.T(language,"boosting"));
            restoreButton.Text=Texts.T(language,"restore");
            junkButton.Text=Texts.T(language,"junk");
            tweaksButton.Text=Texts.T(language,"tweaks");
            agentButton.Text=Texts.T(language,"agent");
            tray.Text=Texts.Product;
            BuildTrayMenu();
            toolTip.SetToolTip(refresh,Texts.T(language,"refresh"));
            toolTip.SetToolTip(junkButton,Texts.T(language,"junkForm"));
            toolTip.SetToolTip(tweaksButton,Texts.T(language,"tweaksForm"));
            toolTip.SetToolTip(languageButton,Texts.LanguageNames[(int)language]);
        }
        private async Task LoadAsync()
        {
            SetEnabled(false); summary.Text=Texts.T(language,"loading");
            try { var loaded=await Task.Run(() => LoadServices()); entries.RaiseListChangedEvents=false; entries.Clear(); foreach(var item in loaded)entries.Add(item);entries.RaiseListChangedEvents=true;source.ResetBindings(false); if (baselineRunning.Count == 0) foreach (var item in entries) if (item.Status == "Running") baselineRunning[item.Name] = true; UpdateSummary(); }
            catch(Exception ex){summary.Text=ex.Message;} finally{SetEnabled(true);}
        }
        private static bool IsDisabledStart(string name)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name))
                {
                    if (key == null) return false;
                    var value = key.GetValue("Start");
                    return value is int && ((int)value) == 4;
                }
            }
            catch { return false; }
        }
        private static List<ServiceEntry> LoadServices()
        {
            var lookup=ServiceController.GetServices().ToDictionary(s=>s.ServiceName,StringComparer.OrdinalIgnoreCase);
            return Services.Select(name => { ServiceController service; if(!lookup.TryGetValue(name,out service))return new ServiceEntry{Name=name,DisplayName=name,Status="Not installed",Exists=false}; return new ServiceEntry{Name=service.ServiceName,DisplayName=service.DisplayName,Status=Status(service.Status),Exists=true,Disabled=IsDisabledStart(service.ServiceName)}; }).Where(e=>e.Exists).OrderByDescending(e=>e.Status=="Running").ThenBy(e=>e.DisplayName).ToList();
        }
        private async void GridClick(object sender,DataGridViewCellEventArgs e){if(e.RowIndex<0||e.ColumnIndex<0)return; string column=grid.Columns[e.ColumnIndex].Name;if(column=="Start"||column=="Stop")await Change(entries[e.RowIndex],column=="Start");}
        private async void GridHeaderClick(object sender,DataGridViewCellMouseEventArgs e){if(e.ColumnIndex<0)return;string column=grid.Columns[e.ColumnIndex].Name;if(column=="Start"||column=="Stop")await ChangeAll(column=="Start");}
        private void BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem(Texts.T(language, "trayOpen"));
            open.Click += delegate { Show(); WindowState = FormWindowState.Normal; Activate(); };
            var boost = new ToolStripMenuItem(Texts.T(language, "boost"));
            boost.Click += async delegate { if (boostBusy || optimize.IsBusy || boostButton.IsBusy) return; if (restoreButton.Enabled) await RestoreBoost(); else await ApplyBoost(); };
            var exit = new ToolStripMenuItem(Texts.T(language, "trayExit"));
            exit.Click += delegate { Close(); };
            menu.Items.AddRange(new ToolStripItem[] { open, boost, new ToolStripSeparator(), exit });
            tray.ContextMenuStrip = menu;
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x312 && m.WParam.ToInt32() == 1 && !boostBusy && !optimize.IsBusy && !boostButton.IsBusy)
            {
                if (restoreButton.Enabled) { var revert = RestoreBoost(); }
                else { var apply = ApplyBoost(); }
            }
        }
        private void UpdateAutoTimer()
        {
            if (Agent.AutoBoost && Agent.Games.Count > 0) { if (!gameTimer.Enabled) { lastInGame = false; gameTimer.Start(); } }
            else gameTimer.Stop();
        }
        private async void GameTick(object sender, EventArgs e)
        {
            if (!Agent.AutoBoost) return;
            string process = ForegroundProcessName();
            bool inGame = process != null && Agent.Games.Contains(process);
            if (inGame == lastInGame) return;
            lastInGame = inGame;
            if (inGame && !autoBoosted && !boostBusy && !optimize.IsBusy && !boostButton.IsBusy)
            {
                autoBoosted = true;
                await Optimize();
                await ApplyBoost();
            }
            else if (!inGame && autoBoosted && !boostBusy && !optimize.IsBusy)
            {
                autoBoosted = false;
                await RestoreBoost();
            }
        }
        private static string ForegroundProcessName()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle == IntPtr.Zero) return null;
                uint pid;
                GetWindowThreadProcessId(handle, out pid);
                if (pid == 0) return null;
                using (var process = Process.GetProcessById((int)pid))
                    return process.ProcessName.ToLowerInvariant();
            }
            catch { return null; }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        private async Task ChangeAll(bool start)
        {
            List<ServiceEntry> todo;
            if (start)
            {
                // Baseline restore: only restart services that were running when the app loaded.
                todo = entries.Where(e => e.Exists && !e.Busy && baselineRunning.ContainsKey(e.Name) && e.Status != "Running").ToList();
            }
            else
            {
                // Stop: running, non-disabled services. Disabled ones cannot be stopped.
                todo = entries.Where(e => e.Exists && !e.Busy && e.Status == "Running" && !e.Disabled).ToList();
            }
            grid.Enabled=false;
            try
            {
                var gate = new SemaphoreSlim(4);
                var tasks = todo.Select(async item =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        item.Busy = true; item.Status = start ? "Starting..." : "Stopping..."; source.ResetBindings(false);
                        item.Status = await Task.Run(() => Control(item.Name, start));
                    }
                    finally { item.Busy = false; source.ResetBindings(false); UpdateSummary(); gate.Release(); }
                });
                await Task.WhenAll(tasks);
                if (start)
                {
                    // Second pass: services that failed on the first pass may have had
                    // dependencies that are only running now. Retry once, sequentially.
                    var retry = todo.Where(e => e.Status.StartsWith("Failed")).ToList();
                    foreach (var item in retry)
                    {
                        item.Busy = true; item.Status = "Starting..."; source.ResetBindings(false);
                        item.Status = await Task.Run(() => Control(item.Name, true));
                        item.Busy = false; source.ResetBindings(false); UpdateSummary();
                    }
                }
            }
            finally { grid.Enabled=true; }
        }
        private async Task Change(ServiceEntry entry,bool start)
        {
            if(!entry.Exists||entry.Busy)return;entry.Busy=true;entry.Status=start?"Starting...":"Stopping...";source.ResetBindings(false);
            try{entry.Status=await Task.Run(() => Control(entry.Name,start));}catch(Exception ex){entry.Status="Failed: "+ex.Message;}finally{entry.Busy=false;source.ResetBindings(false);UpdateSummary();}
        }
        private string Control(string name,bool start)
        {
            try
            {
                using (var service = new ServiceController(name))
                {
                    service.Refresh();
                    if (start)
                    {
                        if (service.Status == ServiceControllerStatus.Running) return "Running";
                        if (service.Status == ServiceControllerStatus.StopPending) { try { service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8)); } catch { } }
                        service.Start();
                        try { service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8)); } catch (System.TimeoutException) { }
                        service.Refresh();
                        return Status(service.Status);
                    }
                    if (service.Status == ServiceControllerStatus.Stopped) return "Stopped";
                    if (service.Status == ServiceControllerStatus.StopPending)
                    {
                        // Stuck in StopPending: nudge it once more.
                        try { service.Stop(); } catch { }
                    }
                    else
                    {
                        StopDependents(service, 0);
                        service.Stop();
                    }
                    try { service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8)); }
                    catch
                    {
                        try { service.Stop(); service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(3)); } catch { }
                    }
                    service.Refresh();
                    return Status(service.Status);
                }
            }
            catch (System.ComponentModel.Win32Exception wex) { return "Failed: " + Reason(wex); }
            catch (InvalidOperationException iex) { var inner = iex.InnerException as System.ComponentModel.Win32Exception; return "Failed: " + (inner != null ? Reason(inner) : Texts.T(language, "errGeneric")); }
            catch (System.TimeoutException) { return "Failed: " + Texts.T(language, "errUnresponsive"); }
            catch { return "Failed: " + Texts.T(language, "errGeneric"); }
        }
        private void StopDependents(ServiceController service,int depth)
        {
            if (depth > 2) return;
            foreach (var dependent in service.DependentServices)
            {
                try
                {
                    dependent.Refresh();
                    if (dependent.Status != ServiceControllerStatus.Running && dependent.Status != ServiceControllerStatus.StartPending) continue;
                    StopDependents(dependent, depth + 1);
                    dependent.Stop();
                    try { dependent.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5)); } catch { }
                }
                catch { }
                finally { dependent.Dispose(); }
            }
        }
        private string Reason(System.ComponentModel.Win32Exception wex)
        {
            switch (wex.NativeErrorCode)
            {
                case 5: return Texts.T(language, "errDenied");
                case 1051: return Texts.T(language, "errDependents");
                case 1053: return Texts.T(language, "errUnresponsive");
                default: return Texts.T(language, "errGeneric");
            }
        }
        private async Task Optimize()
        {
            if (optimize.IsBusy || boostBusy) return;
            SetEnabled(false);
            ulong before = 0;
            try
            {
                optimize.Begin(Texts.T(language, "measuring"));
                before = await Task.Run(() => MemoryInfo.AvailablePhysicalBytes());

                var todo = entries.Where(e => e.Exists && !e.Busy && !e.Disabled && e.Status != "Stopped").ToList();
                int stoppedCount = 0;
                foreach (var item in todo)
                {
                    item.Busy = true; item.Status = "Stopping..."; source.ResetBindings(false);
                    double p = todo.Count == 0 ? 0 : (double)stoppedCount / todo.Count;
                    optimize.SetProgress(p * 0.78 + 0.0, Texts.T(language, "stoppingServices") + " " + item.Name);
                    try { item.Status = await Task.Run(() => Control(item.Name, false)); }
                    catch (Exception ex) { item.Status = "Failed: " + ex.Message; }
                    finally { item.Busy = false; if (item.Status == "Stopped") stoppedCount++; source.ResetBindings(false); UpdateSummary(); }
                }

                optimize.SetProgress(0.85, Texts.T(language, "flushingMemory"));
                await Task.Run(() => MemoryCleaner.PurgeAll());

                optimize.SetProgress(0.97, Texts.T(language, "measuring"));
                ulong after = await Task.Run(() => MemoryInfo.AvailablePhysicalBytes());
                long freed = (long)after - (long)before;
                double freedMb = freed / 1024.0 / 1024.0;
                string label = freedMb >= 1024 ? string.Format("{0:0.#} GB", freedMb / 1024.0) : string.Format("{0:#,0} MB", (int)freedMb);
                optimize.Finish(string.Format(Texts.T(language, "ramFreedFmt"), label));
                summary.Text = string.Format(Texts.T(language, "sessionFmt"), stoppedCount, label);
            }
            catch (Exception ex) { summary.Text = ex.Message; }
            finally { SetEnabled(true); }
        }
        private async Task ApplyBoost()
        {
            if (boostBusy || boostButton.IsBusy || optimize.IsBusy) return;
            if (prevScheme != Guid.Empty || timerBoosted || ramMonitor != null) { boostButton.Finish(Texts.T(language, "boostActive")); return; }
            boostBusy = true; SetEnabled(false);
            try
            {
                boostButton.SetLabels(Texts.T(language, "boost"), Texts.T(language, "boosting"));
                boostButton.Begin(Texts.T(language, "powerPhase"));
                var scheme = await Task.Run(() => { Guid previous, created; Boost.ApplyUltimatePower(out previous, out created); return Tuple.Create(previous, created); });
                prevScheme = scheme.Item1; createdScheme = scheme.Item2;
                boostButton.SetProgress(0.4, Texts.T(language, "gpuPhase"));
                gpuState = await Task.Run(() => Boost.SetGpuMaxPerformance());
                boostButton.SetProgress(0.6, Texts.T(language, "timerPhase"));
                // NtSetTimerResolution binds the request to the calling thread, so it must
                // run on the UI thread (which lives for the app's lifetime), not Task.Run.
                int prevRes; bool started = Boost.StartTimerResolution(out prevRes);
                timerBoosted = started; prevTimerResolution = prevRes;
                boostButton.SetProgress(0.7, Texts.T(language, "tweaksPhase"));
                await Task.Run(delegate { for (int i = 0; i < 6; i++) if (!GameTweaks.Effective(i)) { try { GameTweaks.Apply(i); } catch { } } });
                boostButton.SetProgress(0.85, Texts.T(language, "monitorPhase"));
                if (ramMonitor == null) ramMonitor = new RamMonitor(m =>
                {
                    try { if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(() => summary.Text = string.Format(Texts.T(language, "monitorFreedFmt"), m))); }
                    catch { }
                });
                ramMonitor.Start();
                boostButton.SetProgress(1.0, "");
                boostButton.Finish(Texts.T(language, "boostActive"));
                restoreButton.Enabled = true;
                boostNotice.Text = Texts.T(language, "boostNotice");
                boostNotice.Visible = true;
                RecoveryState.Save(prevScheme, createdScheme, gpuState == GpuBoost.NvidiaApplied);
                GameTweaks.SaveJournal();
                if (gpuState == GpuBoost.AmdGuidance) MessageBox.Show(Texts.T(language, "amdGpuTip"), Texts.Product, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { summary.Text = ex.Message; boostButton.Finish(Texts.T(language, "failed")); }
            finally { boostBusy = false; SetEnabled(true); }
        }
        private async Task RestoreBoost()
        {
            if (boostBusy || boostButton.IsBusy || optimize.IsBusy) return;
            if (ramMonitor == null && !timerBoosted && gpuState != GpuBoost.NvidiaApplied && prevScheme == Guid.Empty && createdScheme == Guid.Empty) return;
            boostBusy = true; restoreButton.Enabled = false; SetEnabled(false);
            boostNotice.Visible = false;
            try
            {
                boostButton.SetLabels(Texts.T(language, "restore"), Texts.T(language, "restoring"));
                boostButton.Begin(Texts.T(language, "monitorPhase"));
                if (ramMonitor != null) { ramMonitor.Dispose(); ramMonitor = null; }
                boostButton.SetProgress(0.3, Texts.T(language, "timerPhase"));
                // Same UI-thread requirement as ApplyBoost: the resolution request is
                // bound to the calling thread, so restore it here, not on Task.Run.
                if (timerBoosted) { Boost.StopTimerResolution(prevTimerResolution); timerBoosted = false; }
                boostButton.SetProgress(0.6, Texts.T(language, "gpuPhase"));
                if (gpuState == GpuBoost.NvidiaApplied) { await Task.Run(() => Boost.RestoreGpu()); gpuState = GpuBoost.None; }
                boostButton.SetProgress(0.9, Texts.T(language, "powerPhase"));
                Guid scheme = prevScheme, created = createdScheme;
                if (scheme != Guid.Empty || created != Guid.Empty) { await Task.Run(() => Boost.RestorePower(scheme, created)); prevScheme = Guid.Empty; createdScheme = Guid.Empty; }
                await Task.Run(() => GameTweaks.RevertAll());
                RecoveryState.Clear();
                boostButton.SetProgress(1.0, "");
                boostButton.Finish(Texts.T(language, "boostRestored"));
                boostButton.SetLabels(Texts.T(language, "boost"), Texts.T(language, "boosting"));
                UpdateSummary();
            }
            catch (Exception ex) { summary.Text = ex.Message; boostButton.Finish(Texts.T(language, "failed")); }
            finally
            {
                boostBusy = false; SetEnabled(true);
                restoreButton.Enabled = prevScheme != Guid.Empty || createdScheme != Guid.Empty || timerBoosted || gpuState == GpuBoost.NvidiaApplied || ramMonitor != null;
            }
        }
        private void CleanupBoost(object sender, FormClosingEventArgs e)
        {
            if (boostBusy && !cleaningUp && e.CloseReason == CloseReason.UserClosing)
            {
                // A boost/restore operation is mid-flight on await Task.Run stages.
                // Blocking the UI thread would starve its continuations and prevent
                // the state fields from being recorded, so cancel this close, let the
                // stages settle via an async wait, restore everything, and re-close.
                e.Cancel = true;
                BeginInvoke(new Action(async delegate
                {
                    cleaningUp = true;
                    try
                    {
                        var deadline = DateTime.UtcNow.AddSeconds(8);
                        while (boostBusy && DateTime.UtcNow < deadline) await Task.Delay(100);
                    }
                    catch { }
                    RestoreBoostSync();
                    cleaningUp = false;
                    if (!IsDisposed) Close();
                }));
                return;
            }
if (e.CloseReason == CloseReason.UserClosing && !cleaningUp && !exitConfirmed &&
                (prevScheme != Guid.Empty || timerBoosted || ramMonitor != null))
            {
                e.Cancel = true;
                var choice = MessageBox.Show(this, Texts.T(language, "exitWarn"), Texts.T(language, "exitWarnTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (choice == DialogResult.Yes) { exitConfirmed = true; Close(); }
                return;
            }
            RestoreBoostSync();
        }
        private void RestoreBoostSync()
        {
            try
            {
                if (ramMonitor != null) { ramMonitor.Dispose(); ramMonitor = null; }
                if (timerBoosted) { Boost.StopTimerResolution(prevTimerResolution); timerBoosted = false; }
                if (gpuState == GpuBoost.NvidiaApplied) { Boost.RestoreGpu(); gpuState = GpuBoost.None; }
                if (prevScheme != Guid.Empty || createdScheme != Guid.Empty) { Boost.RestorePower(prevScheme, createdScheme); prevScheme = Guid.Empty; createdScheme = Guid.Empty; }
                GameTweaks.RevertAll();
                RecoveryState.Clear();
            }
            catch { }
        }
        private void UpdateSummary(){int active=entries.Count(e=>e.Exists&&e.Status=="Running"),total=entries.Count(e=>e.Exists);summary.Text=string.Format(Texts.T(language,"activeFmt"),active,total);}
        private void SetEnabled(bool value){refresh.Enabled=value;languageButton.Enabled=value;grid.Enabled=value;tweaksButton.Enabled=value;agentButton.Enabled=value;}
        private void GridFormat(object sender,DataGridViewCellFormattingEventArgs e)
        {
            if(grid.Columns[e.ColumnIndex].Name!="Status")return;string s=Convert.ToString(e.Value);if(language!=AppLanguage.English){string key=s=="Running"?"statusRunning":s=="Stopped"?"statusStopped":s=="Not installed"?"statusNotInstalled":s=="Starting..."?"statusStarting":s=="Stopping..."?"statusStopping":s=="Paused"?"statusPaused":null;if(key!=null)e.Value=Texts.T(language,key);}if(s=="Running")e.CellStyle.ForeColor=Color.FromArgb(74,245,174);else if(s=="Stopped")e.CellStyle.ForeColor=Color.FromArgb(155,185,194);else if(s=="Not installed")e.CellStyle.ForeColor=Color.FromArgb(255,180,81);else if(s.StartsWith("Failed"))e.CellStyle.ForeColor=Color.FromArgb(255,103,111);
        }
        private void GridPaint(object sender,DataGridViewCellPaintingEventArgs e)
        {
            if(e.ColumnIndex<0)return;string name=grid.Columns[e.ColumnIndex].Name;if(name!="Start"&&name!="Stop")return;var color=name=="Start"?Color.FromArgb(0,232,179):Color.FromArgb(255,108,128);e.PaintBackground(e.CellBounds,true);var r=e.RowIndex<0?Rectangle.Inflate(e.CellBounds,-4,-6):Rectangle.Inflate(e.CellBounds,-7,-7);using(var brush=new SolidBrush(name=="Start"?Color.FromArgb(11,65,62):Color.FromArgb(70,26,39)))using(var pen=new Pen(color,1)){e.Graphics.FillRectangle(brush,r);e.Graphics.DrawRectangle(pen,r);}TextRenderer.DrawText(e.Graphics,e.RowIndex<0?grid.Columns[e.ColumnIndex].HeaderText:Convert.ToString(e.FormattedValue),e.RowIndex<0?grid.ColumnHeadersDefaultCellStyle.Font:grid.DefaultCellStyle.Font,r,e.RowIndex<0?color:Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);e.Handled=true;
        }
        private static string Status(ServiceControllerStatus value){switch(value){case ServiceControllerStatus.Running:return"Running";case ServiceControllerStatus.Stopped:return"Stopped";case ServiceControllerStatus.StartPending:return"Starting...";case ServiceControllerStatus.StopPending:return"Stopping...";case ServiceControllerStatus.Paused:return"Paused";default:return value.ToString();}}
    }
}
