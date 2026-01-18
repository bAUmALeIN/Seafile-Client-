using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Helper;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class Form1 : MaterialForm
    {
        private SeafileClient _seafileClient;
        private NavigationState _navState;
        private readonly string _authToken;
        private DownloadManager _downloadManager;

        private MaterialTabControl _tabControl;
        private MaterialTabSelector _tabSelector;
        private System.Windows.Forms.TabPage _tabFiles;
        private System.Windows.Forms.TabPage _tabDownloads;
        private MaterialListView _lstDownloads;

        private FlowLayoutPanel _flowPath;
        private ImageList _repoIcons;
        private PictureBox _appIcon;

        // WICHTIG: Schriftarten Global
        private readonly Font _crumbFontBold = new Font("Segoe UI", 11f, FontStyle.Bold);

        public Form1(string token)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();
            _authToken = token;
            _navState = new NavigationState();
            _seafileClient = new SeafileClient(_authToken);

            _downloadManager = new DownloadManager(_seafileClient);
            _downloadManager.OnDownloadStarted += AddDownloadToUi;
            _downloadManager.OnItemUpdated += UpdateDownloadInUi;

            SetupMaterialSkin();

            InitializeIcons();
            InitializeTabs();
            InitializeCustomUI();
            InitializeLogo(); // Logo Setup am Ende
        }

        // =========================================================================
        // HELPER KLASSE: Stabilisiertes Label
        // =========================================================================
        private class StableLabel : Label
        {
            public StableLabel()
            {
                this.AutoSize = false;
                this.UseMnemonic = false;
                this.TextAlign = ContentAlignment.MiddleLeft;
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                if (this.Font != null && !this.Font.Bold && this.Text != "/")
                {
                    this.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                }
            }
        }

        private void SetupMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;

            materialSkinManager.ColorScheme = new MaterialColorScheme(
                MaterialPrimary.BlueGrey800,
                MaterialPrimary.BlueGrey900,
                MaterialPrimary.BlueGrey500,
                MaterialAccent.Orange400,
                MaterialTextShade.WHITE
            );
        }

        // =========================================================================
        // NEU: LOGO RECHTS IN DER ACTIONBAR
        // =========================================================================
        private void InitializeLogo()
        {
            // 1. Panel suchen (es liegt jetzt in tabFiles)
            Control actionPanel = null;
            if (_tabFiles.Controls.ContainsKey("panelActionbar"))
                actionPanel = _tabFiles.Controls["panelActionbar"];
            else if (Controls.ContainsKey("panelActionbar"))
                actionPanel = Controls["panelActionbar"];

            if (actionPanel == null) return;

            _appIcon = new PictureBox();
            try { _appIcon.Image = Properties.Resources.app_logo; }
            catch { _appIcon.Image = Properties.Resources.icon_repo; }

            _appIcon.SizeMode = PictureBoxSizeMode.Zoom;

            // TRICK: Da das Parent jetzt das Panel ist, funktioniert Transparent!
            _appIcon.BackColor = Color.Transparent;

            _appIcon.Size = new Size(40, 40);

            // Position: Ganz rechts im Panel
            int x = actionPanel.Width - _appIcon.Width - 10;
            int y = (actionPanel.Height - _appIcon.Height) / 2;

            _appIcon.Location = new Point(x, y);
            _appIcon.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Klebt rechts fest

            // Wichtig: Zum Panel hinzufügen, nicht zur Form!
            actionPanel.Controls.Add(_appIcon);
            _appIcon.BringToFront();

            // Titel ohne Leerzeichen, da Logo jetzt rechts ist
            this.Text = "BBS-ME File Explorer";
        }

        private void InitializeIcons()
        {
            _repoIcons = new ImageList();
            _repoIcons.ImageSize = new Size(24, 24);
            _repoIcons.ColorDepth = ColorDepth.Depth32Bit;
            _repoIcons.Images.Add("dir", Properties.Resources.icon_folder);
            _repoIcons.Images.Add("file", Properties.Resources.icon_file);
            _repoIcons.Images.Add("repo", Properties.Resources.icon_repo);
            _repoIcons.Images.Add("back", Properties.Resources.icon_back);
        }

        private void InitializeTabs()
        {
            Color darkBackground = Color.FromArgb(50, 50, 50);

            _tabControl = new MaterialTabControl();
            _tabControl.Depth = 0;
            _tabControl.Location = new Point(0, 112);
            _tabControl.MouseState = MaterialDrawHelper.MaterialMouseState.HOVER;
            _tabControl.Name = "tabControl1";
            _tabControl.SelectedIndex = 0;
            _tabControl.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 112);
            _tabControl.TabIndex = 100;
            _tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _tabFiles = new System.Windows.Forms.TabPage();
            _tabFiles.Text = "Dateien";
            _tabFiles.BackColor = darkBackground;

            _tabDownloads = new System.Windows.Forms.TabPage();
            _tabDownloads.Text = "Transfers";
            _tabDownloads.BackColor = darkBackground;

            _tabControl.Controls.Add(_tabFiles);
            _tabControl.Controls.Add(_tabDownloads);

            this.Controls.Add(_tabControl);

            _tabSelector = new MaterialTabSelector();
            _tabSelector.BaseTabControl = _tabControl;
            _tabSelector.Depth = 0;
            _tabSelector.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            _tabSelector.Location = new Point(0, 64);
            _tabSelector.MouseState = MaterialDrawHelper.MaterialMouseState.HOVER;
            _tabSelector.Name = "tabSelector1";
            _tabSelector.Size = new Size(this.ClientSize.Width, 48);
            _tabSelector.TabIndex = 99;
            _tabSelector.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _tabSelector.CharacterCasing = MaterialTabSelector.CustomCharacterCasing.Normal;

            this.Controls.Add(_tabSelector);

            if (lstRepos != null && !lstRepos.IsDisposed)
            {
                if (this.Controls.Contains(lstRepos)) this.Controls.Remove(lstRepos);
                lstRepos.Parent = _tabFiles;
                lstRepos.Dock = DockStyle.Fill;
                lstRepos.Visible = true;
                lstRepos.BackColor = darkBackground;
                lstRepos.SmallImageList = _repoIcons;
            }

            if (Controls.ContainsKey("panelActionbar"))
            {
                var pnl = Controls["panelActionbar"];
                this.Controls.Remove(pnl);
                pnl.Parent = _tabFiles;
                pnl.Dock = DockStyle.Top;
                pnl.Height = 60;
                lstRepos.BringToFront();
                InitializeBreadcrumbs(pnl);
            }

            _lstDownloads = new MaterialListView();
            _lstDownloads.Parent = _tabDownloads;
            _lstDownloads.Dock = DockStyle.Fill;
            _lstDownloads.Depth = 0;
            _lstDownloads.MouseState = MaterialDrawHelper.MaterialMouseState.HOVER;
            _lstDownloads.BorderStyle = BorderStyle.None;
            _lstDownloads.FullRowSelect = true;
            _lstDownloads.OwnerDraw = true;
            _lstDownloads.View = View.Details;
            _lstDownloads.BackColor = darkBackground;

            _lstDownloads.Columns.Clear();
            _lstDownloads.Columns.Add("Datei / Ordner", 400);
            _lstDownloads.Columns.Add("Status", 200);
            _lstDownloads.Columns.Add("Fortschritt", 100);
            _lstDownloads.Columns.Add("Startzeit", 120);

            UiHelper.SetupListView(_lstDownloads);

            ImageList downloadIcons = new ImageList();
            downloadIcons.ImageSize = new Size(24, 24);
            downloadIcons.ColorDepth = ColorDepth.Depth32Bit;
            downloadIcons.Images.Add("upload", Properties.Resources.icon_upload);
            downloadIcons.Images.Add("download", Properties.Resources.icon_download);
            _lstDownloads.SmallImageList = downloadIcons;

            _lstDownloads.SizeChanged += (s, e) => UiHelper.UpdateColumnWidths(_lstDownloads);
            _lstDownloads.ColumnWidthChanging += (s, e) => { e.Cancel = true; e.NewWidth = _lstDownloads.Columns[e.ColumnIndex].Width; };
        }

        private void ResizeDownloadListColumns()
        {
            if (_lstDownloads.Columns.Count < 4 || _lstDownloads.ClientSize.Width == 0) return;
            int fixedWidth = _lstDownloads.Columns[0].Width + _lstDownloads.Columns[2].Width + _lstDownloads.Columns[3].Width;
            int availableWidth = _lstDownloads.ClientSize.Width - fixedWidth - 20;
            if (availableWidth > 50) _lstDownloads.Columns[1].Width = availableWidth;
        }

        private void InitializeBreadcrumbs(Control parentPanel)
        {
            _flowPath = new FlowLayoutPanel();
            _flowPath.AutoSize = true;
            _flowPath.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _flowPath.BackColor = Color.Transparent;
            _flowPath.FlowDirection = FlowDirection.LeftToRight;
            _flowPath.WrapContents = false;
            _flowPath.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            parentPanel.Controls.Add(_flowPath);
            _flowPath.Location = new Point(260, 15);
        }

        private void UpdateBreadcrumbs(string searchContext = null)
        {
            if (_flowPath == null) return;

            _flowPath.SuspendLayout();
            _flowPath.Controls.Clear();

            Color colorLink = Color.White;
            Color colorSep = Color.Gray;

            PictureBox btnHomeIcon = CreateHomeIcon();
            _flowPath.Controls.Add(btnHomeIcon);

            Label lblHomeText = CreateBreadcrumbLabel("Bibliotheken", null, _crumbFontBold, colorLink);
            _flowPath.Controls.Add(lblHomeText);

            if (searchContext != null)
            {
                AddBreadcrumbSeparator(_crumbFontBold, colorSep);
                Label lblSearch = CreateBreadcrumbLabel($"Suche: '{searchContext}'", null, _crumbFontBold, Color.Orange);
                _flowPath.Controls.Add(lblSearch);
            }
            else if (!_navState.IsInRoot)
            {
                AddBreadcrumbSeparator(_crumbFontBold, colorSep);
                Label lblRepo = CreateBreadcrumbLabel(_navState.CurrentRepoName, "/", _crumbFontBold, colorLink);
                _flowPath.Controls.Add(lblRepo);

                string path = _navState.CurrentPath;
                if (path != "/")
                {
                    string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    string currentBuildPath = "";
                    foreach (var part in parts)
                    {
                        currentBuildPath += "/" + part;
                        AddBreadcrumbSeparator(_crumbFontBold, colorSep);
                        Label lblPart = CreateBreadcrumbLabel(part, currentBuildPath, _crumbFontBold, colorLink);
                        _flowPath.Controls.Add(lblPart);
                    }
                }
            }

            _flowPath.ResumeLayout(true);
        }

        private PictureBox CreateHomeIcon()
        {
            PictureBox pb = new PictureBox();
            try { pb.Image = Properties.Resources.icon_home; } catch { }
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Size = new Size(24, 30);
            pb.Cursor = Cursors.Hand;
            pb.Margin = new Padding(0, 0, 5, 0);
            pb.Click += async (s, e) => { _navState.ResetToRoot(); await LadeInhalt(); };
            return pb;
        }

        private Label CreateBreadcrumbLabel(string text, string navigationPath, Font font, Color color)
        {
            StableLabel lbl = new StableLabel();
            lbl.Text = text;
            lbl.ForeColor = color;
            lbl.Font = font;
            lbl.Height = 30;
            lbl.Margin = new Padding(0, 0, 0, 0);

            Size preferredSize = TextRenderer.MeasureText(text, font);
            lbl.Width = preferredSize.Width + 5;

            bool isClickable = !text.StartsWith("Suche:");

            if (isClickable)
            {
                lbl.Cursor = Cursors.Hand;
                lbl.MouseEnter += (s, e) => lbl.ForeColor = Color.Orange;
                lbl.MouseLeave += (s, e) => lbl.ForeColor = color;
                lbl.Click += async (s, e) =>
                {
                    if (navigationPath == null) { _navState.ResetToRoot(); await LadeInhalt(); }
                    else if (navigationPath == "/") { _navState.EnterRepo(_navState.CurrentRepoId, _navState.CurrentRepoName); await LadeInhalt(); }
                    else { _navState.EnterRepo(_navState.CurrentRepoId, _navState.CurrentRepoName); _navState.EnterFolder(navigationPath.TrimStart('/')); await LadeInhalt(); }
                };
            }
            return lbl;
        }

        private void AddBreadcrumbSeparator(Font font, Color color)
        {
            StableLabel sep = new StableLabel();
            sep.Text = "/";
            sep.ForeColor = color;
            sep.Font = font;
            sep.Height = 30;
            sep.Width = 15;
            sep.TextAlign = ContentAlignment.MiddleCenter;
            sep.Margin = new Padding(0, 0, 0, 0);
            _flowPath.Controls.Add(sep);
        }

        private void InitializeCustomUI()
        {
            ContextMenuStrip ctxMenu = MenuBuilder.CreateContextMenu(CtxDownload_Click, BtnDelete_Click);
            ToolStripMenuItem itemJump = new ToolStripMenuItem("Im Ordner anzeigen");
            itemJump.Click += CtxJumpTo_Click;
            itemJump.Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_jump, 16, 16);
            ctxMenu.Items.Insert(0, new ToolStripSeparator());
            ctxMenu.Items.Insert(0, itemJump);

            if (ctxMenu.Items.Count > 2) ctxMenu.Items[2].Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_download, 16, 16);
            if (ctxMenu.Items.Count > 4) ctxMenu.Items[4].Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_löschen, 16, 16);

            lstRepos.ContextMenuStrip = ctxMenu;
            UiHelper.SetupListView(lstRepos, _repoIcons);
            lstRepos.Columns.Clear();
            lstRepos.Columns.Add("Name", 400);
            lstRepos.Columns.Add("Größe", 90);
            lstRepos.Columns.Add("Geändert", 160);
            lstRepos.Columns.Add("Typ / Pfad", 150);
            lstRepos.DoubleClick += lstRepos_DoubleClick;
            lstRepos.SizeChanged += (s, e) => UiHelper.UpdateColumnWidths(lstRepos);
            lstRepos.ColumnWidthChanging += (s, e) => { e.Cancel = true; e.NewWidth = lstRepos.Columns[e.ColumnIndex].Width; };
            lstRepos.AllowDrop = true;

            lstRepos.DragEnter += LstRepos_DragEnter;
            lstRepos.DragDrop += LstRepos_DragDrop;

            ReplaceMaterialButtonsWithStandard();

            _lstDownloads.DoubleClick += _lstDownloads_DoubleClick;

            try { if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].BackColor = Color.FromArgb(45, 45, 48); } catch { }
        }

        private void ReplaceMaterialButtonsWithStandard()
        {
            Control actionPanel = null;
            if (Controls.ContainsKey("panelActionbar")) actionPanel = Controls["panelActionbar"];
            else if (_tabFiles.Controls.ContainsKey("panelActionbar")) actionPanel = _tabFiles.Controls["panelActionbar"];

            if (actionPanel == null) return;

            var toRemove = new List<Control>();
            foreach (Control c in actionPanel.Controls)
            {
                if (c is MaterialButton || c.Name.StartsWith("materialButton") || c.Name == "btnLogout")
                    toRemove.Add(c);
            }
            foreach (var c in toRemove) actionPanel.Controls.Remove(c);

            // =========================================================
            // LINKS
            // =========================================================
            int leftX = 10;
            int btnY = 12;

            System.Windows.Forms.Button btnNew = CreateFlatButton("NEU", Properties.Resources.icon_new);
            btnNew.Location = new Point(leftX, btnY);
            btnNew.Click += BtnNew_Click;
            actionPanel.Controls.Add(btnNew);

            System.Windows.Forms.Button btnDel = CreateFlatButton("LÖSCHEN", Properties.Resources.icon_delete);
            btnDel.Location = new Point(btnNew.Right + 10, btnY);
            btnDel.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDel);

            // =========================================================
            // RECHTS (Angepasst für Logo Platz)
            // =========================================================

            // Logo ist ca. 40-50px breit. Wir schieben die Buttons um 60px nach links vom Rand.
            int rightEdge = actionPanel.Width - 60;

            System.Windows.Forms.Button btnOut = CreateFlatButton("AUSLOGGEN", Properties.Resources.icon_logout);
            btnOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOut.Location = new Point(rightEdge - btnOut.Width, btnY);
            btnOut.Click += btnLogout_Click;
            actionPanel.Controls.Add(btnOut);

            System.Windows.Forms.Button btnSearch = CreateFlatButton("SUCHEN", Properties.Resources.icon_search);
            btnSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSearch.Location = new Point(btnOut.Left - btnSearch.Width - 10, btnY);
            btnSearch.Click += btnSearch_Click;
            actionPanel.Controls.Add(btnSearch);
        }

        private System.Windows.Forms.Button CreateFlatButton(string text, Image icon)
        {
            System.Windows.Forms.Button btn = new System.Windows.Forms.Button();

            btn.Text = " " + text;
            btn.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btn.ForeColor = Color.White;
            btn.BackColor = Color.Transparent;

            btn.Image = ResizeImage(icon, 24, 24);
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.TextImageRelation = TextImageRelation.ImageBeforeText;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);

            btn.Height = 38;
            btn.Cursor = Cursors.Hand;

            Size textSize = TextRenderer.MeasureText(btn.Text, btn.Font);
            btn.Width = 24 + 10 + textSize.Width + 10;

            return btn;
        }

        private Image ResizeImage(Image img, int w, int h)
        {
            return new Bitmap(img, new Size(w, h));
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "Lade Bibliotheken...";
            await LadeInhalt();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lstRepos != null) UiHelper.UpdateColumnWidths(lstRepos);
            if (_lstDownloads != null) ResizeDownloadListColumns();
        }

        private async void CtxJumpTo_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            var tag = lstRepos.SelectedItems[0].Tag;
            if (tag is Tuple<string, SeafileEntry> searchResult)
            {
                string repoId = searchResult.Item1;
                SeafileEntry entry = searchResult.Item2;
                string targetPath = entry.parent_dir;
                if (string.IsNullOrEmpty(targetPath)) targetPath = "/";
                _navState.EnterRepo(repoId, "...");
                if (targetPath == "/") _navState.ResetToRoot();
                else { _navState.EnterRepo(repoId, "Bibliothek"); foreach (var part in targetPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)) { _navState.EnterFolder(part); } }
                if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].Enabled = true;
                lstRepos.Visible = true;
                await LadeInhalt();
                foreach (ListViewItem item in lstRepos.Items) { if (item.Tag is SeafileEntry dirEntry && dirEntry.name == entry.name) { item.Selected = true; item.EnsureVisible(); break; } }
            }
            else { UiHelper.ShowInfoDialog("Info", "Du bist bereits in diesem Verzeichnis."); }
        }

        private void AddDownloadToUi(DownloadItem item)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<DownloadItem>(AddDownloadToUi), item); return; }

            string cleanName = item.FileName.Replace("⬇", "").Replace("⬆", "").Trim();
            var lvi = new ListViewItem(cleanName);
            lvi.SubItems.Add(item.Status);
            lvi.SubItems.Add(item.Progress + "%");
            lvi.SubItems.Add(item.StartTime.ToShortTimeString());

            if (item.Type == "Upload") lvi.ImageKey = "upload";
            else lvi.ImageKey = "download";

            item.Tag = lvi;
            lvi.Tag = item;
            _lstDownloads.Items.Add(lvi);
            ResizeDownloadListColumns();
        }

        private void UpdateDownloadInUi(DownloadItem item)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<DownloadItem>(UpdateDownloadInUi), item); return; }
            if (item.Tag is ListViewItem lvi)
            {
                lvi.SubItems[1].Text = item.Status;
                lvi.SubItems[2].Text = item.Progress + "%";
                if (item.Status == "Fertig") lvi.ForeColor = Color.LightGreen;
                else if (item.Status.StartsWith("Fehler") || item.Status == "Abgebrochen") lvi.ForeColor = Color.Salmon;
            }
        }

        private async Task LadeInhalt()
        {
            try
            {
                UpdateBreadcrumbs(); // Reset normal
                if (_seafileClient == null) _seafileClient = new SeafileClient(_authToken);
                lstRepos.Items.Clear(); lstRepos.Groups.Clear(); lstRepos.ShowGroups = true;
                if (_navState.IsInRoot) await LoadLibraries();
                else await LoadDirectory(_navState.CurrentRepoId, _navState.CurrentPath);
                UiHelper.UpdateColumnWidths(lstRepos);
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Ladefehler", ex.Message); }
        }

        private async Task LoadLibraries()
        {
            ListViewGroup grpMine = new ListViewGroup("   MEINE BIBLIOTHEKEN", HorizontalAlignment.Left);
            ListViewGroup grpShared = new ListViewGroup("   FÜR MICH FREIGEGEBEN", HorizontalAlignment.Left);
            lstRepos.Groups.Add(grpMine);
            lstRepos.Groups.Add(grpShared);

            var repos = await _seafileClient.GetLibrariesAsync();
            foreach (var repo in repos)
            {
                var item = new ListViewItem(repo.name, "repo");
                item.SubItems.Add(UiHelper.FormatByteSize(repo.size));
                item.SubItems.Add(FormatDate(repo.mtime));

                string ownerDisplay = repo.type == "grepo" ? "Gruppe" : (repo.owner ?? "-");
                item.SubItems.Add(ownerDisplay);
                item.Tag = repo;

                if (repo.type == "repo") item.Group = grpMine;
                else if (repo.type == "srepo") item.Group = grpShared;
                else if (repo.type == "grepo")
                {
                    string groupName = "   GRUPPE: " + repo.owner.ToUpper();
                    ListViewGroup targetGroup = null;
                    foreach (ListViewGroup existingGroup in lstRepos.Groups)
                    {
                        if (existingGroup.Header == groupName) { targetGroup = existingGroup; break; }
                    }
                    if (targetGroup == null)
                    {
                        targetGroup = new ListViewGroup(groupName, HorizontalAlignment.Left);
                        lstRepos.Groups.Add(targetGroup);
                    }
                    item.Group = targetGroup;
                }
                else item.Group = grpShared;

                lstRepos.Items.Add(item);
            }
        }

        private async Task LoadDirectory(string repoId, string path)
        {
            lstRepos.ShowGroups = false;
            var entries = await _seafileClient.GetDirectoryEntriesAsync(repoId, path);
            var backItem = new ListViewItem(".. [Zurück]", "back");
            backItem.Tag = new SeafileEntry { type = "back" };
            lstRepos.Items.Add(backItem);

            foreach (var entry in entries)
            {
                string iconKey = entry.type == "dir" ? "dir" : "file";
                var item = new ListViewItem(entry.name, iconKey);
                item.SubItems.Add(entry.type == "dir" ? "-" : (entry.size / 1024) + " KB");
                item.SubItems.Add(FormatDate(entry.mtime));
                item.SubItems.Add(entry.type);
                item.Tag = entry;
                lstRepos.Items.Add(item);
            }
            lblStatus.Text = $"{_navState.CurrentRepoName}: {_navState.CurrentPath}";
        }

        private async void CtxDownload_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;

            if (lstRepos.SelectedItems.Count > 1)
            {
                var itemsToDownload = new List<object>();
                foreach (ListViewItem lvi in lstRepos.SelectedItems)
                {
                    if (lvi.Tag is SeafileEntry se && se.type == "back") continue;
                    itemsToDownload.Add(lvi.Tag);
                }

                if (itemsToDownload.Count > 0)
                {
                    string zipName = $"Download_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                    _ = _downloadManager.DownloadMultipleFilesAsZipAsync(itemsToDownload, _navState.CurrentRepoId, zipName);
                }
            }
            else
            {
                var tag = lstRepos.SelectedItems[0].Tag;
                try
                {
                    bool started = false;
                    if (tag is SeafileRepo repo)
                    {
                        _ = _downloadManager.DownloadRepoAsync(repo);
                        started = true;
                    }
                    else
                    {
                        SeafileEntry entryToDownload = null;
                        string repoId = _navState.CurrentRepoId;
                        string navPath = _navState.CurrentPath;

                        if (tag is Tuple<string, SeafileEntry> tuple)
                        {
                            repoId = tuple.Item1;
                            entryToDownload = tuple.Item2;
                            navPath = entryToDownload.parent_dir ?? "/";
                        }
                        else if (tag is SeafileEntry entry)
                        {
                            entryToDownload = entry;
                        }

                        if (entryToDownload != null && entryToDownload.type != "back")
                        {
                            _ = _downloadManager.DownloadEntryAsync(entryToDownload, repoId, navPath);
                            started = true;
                        }
                    }

                    if (started)
                    {
                        MaterialSnackBar snack = new MaterialSnackBar("Download gestartet!", "OK", true);
                        snack.Show(this);
                    }
                }
                catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
            }
        }

        private async void BtnNew_Click(object sender, EventArgs e)
        {
            try
            {
                if (_navState.IsInRoot)
                {
                    string libName = UiHelper.ShowInputDialog("Neue Bibliothek", "Name:");
                    if (!string.IsNullOrWhiteSpace(libName)) { bool success = await _seafileClient.CreateLibraryAsync(libName); if (success) { await LadeInhalt(); UiHelper.ShowSuccessDialog("Erfolg", "Bibliothek erstellt."); } }
                }
                else
                {
                    string folderName = UiHelper.ShowInputDialog("Neuer Ordner", "Name:");
                    if (!string.IsNullOrWhiteSpace(folderName)) { string newPath = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + folderName : _navState.CurrentPath + "/" + folderName; bool success = await _seafileClient.CreateDirectoryAsync(_navState.CurrentRepoId, newPath); if (success) await LadeInhalt(); }
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;

            int count = lstRepos.SelectedItems.Count;
            string title = "LÖSCHEN BESTÄTIGEN";
            string message = "";

            if (count == 1)
            {
                var tag = lstRepos.SelectedItems[0].Tag;
                if (tag is SeafileEntry entry)
                {
                    if (entry.type == "back") return;
                    message = $"Möchten Sie '{entry.name}' wirklich löschen?";
                }
                else if (tag is SeafileRepo repo)
                {
                    message = $"Möchten Sie die Bibliothek '{repo.name}' wirklich löschen?";
                }
            }
            else
            {
                message = $"Möchten Sie wirklich {count} Elemente unwiderruflich löschen?";
            }

            if (!UiHelper.ShowDangerConfirmation(title, message)) return;

            int successCount = 0;
            try
            {
                foreach (ListViewItem item in lstRepos.SelectedItems)
                {
                    var tag = item.Tag;
                    bool success = false;

                    if (tag is SeafileEntry entry && entry.type != "back")
                    {
                        bool isDir = entry.type == "dir";
                        string path = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + entry.name : _navState.CurrentPath + "/" + entry.name;
                        success = await _seafileClient.DeleteEntryAsync(_navState.CurrentRepoId, path, isDir);
                    }
                    else if (tag is SeafileRepo repo)
                    {
                        success = await _seafileClient.DeleteLibraryAsync(repo.id);
                    }

                    if (success) successCount++;
                }

                if (successCount > 0)
                {
                    await LadeInhalt();
                    MaterialSnackBar snack = new MaterialSnackBar($"{successCount} Elemente gelöscht.", "OK", true);
                    snack.Show(this);
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Löschen teilweise fehlgeschlagen", ex.Message); }
        }

        private void btnLogout_Click(object sender, EventArgs e) { if (UiHelper.ShowConfirmationDialog("Abmelden", "Möchtest du dich wirklich abmelden?")) { new DBHelper().DeleteToken(); new AuthManager(null).ClearBrowserCacheOnDisk(); Application.Restart(); } }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = UiHelper.ShowInputDialog("Globale Suche", "Suchbegriff:");
            if (string.IsNullOrWhiteSpace(searchTerm)) { await LadeInhalt(); return; }

            lstRepos.Visible = false;
            lstRepos.Items.Clear();
            lstRepos.Groups.Clear();

            try
            {
                lblStatus.Text = "Suche läuft...";
                UpdateBreadcrumbs("Suche: " + searchTerm);

                var allRepos = await _seafileClient.GetLibrariesAsync();

                lstRepos.BeginUpdate();
                foreach (var repo in allRepos)
                {
                    var entries = await _seafileClient.GetAllFilesRecursiveAsync(repo.id);
                    var matches = entries.Where(x => x.name.ToLower().Contains(searchTerm.ToLower())).ToList();

                    if (matches.Count > 0)
                    {
                        ListViewGroup grp = new ListViewGroup(repo.name, HorizontalAlignment.Left);
                        lstRepos.Groups.Add(grp);

                        foreach (var entry in matches)
                        {
                            string displayPath = (entry.parent_dir ?? "/") + entry.name;
                            var item = new ListViewItem(displayPath, entry.type == "dir" ? "dir" : "file");
                            item.SubItems.Add(entry.type == "dir" ? "-" : UiHelper.FormatByteSize(entry.size));
                            item.SubItems.Add(FormatDate(entry.mtime));
                            item.SubItems.Add(entry.type);
                            item.Tag = new Tuple<string, SeafileEntry>(repo.id, entry);
                            item.Group = grp;
                            lstRepos.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
            finally
            {
                lstRepos.EndUpdate();
                lstRepos.Visible = true;
                lstRepos.Invalidate();
                lblStatus.Text = "Suche beendet.";
                UiHelper.UpdateColumnWidths(lstRepos);
                lstRepos.Invalidate();
            }
        }

        private async void LstRepos_DragDrop(object sender, DragEventArgs e)
        {
            if (_navState.IsInRoot) { UiHelper.ShowInfoDialog("Info", "Bitte öffne erst eine Bibliothek."); return; }
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (droppedPaths != null && droppedPaths.Length > 0)
            {
                try { _ = _downloadManager.UploadFilesAsync(droppedPaths, _navState.CurrentRepoId, _navState.CurrentPath); MaterialSnackBar snack = new MaterialSnackBar("Upload gestartet!", "OK", true); snack.Show(this); }
                catch (Exception ex) { UiHelper.ShowScrollableErrorDialog("Upload Fehler", ex.Message); }
            }
        }

        private async void lstRepos_DoubleClick(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            var tag = lstRepos.SelectedItems[0].Tag;
            if (tag is SeafileRepo repo) { _navState.EnterRepo(repo.id, repo.name); await LadeInhalt(); }
            else if (tag is SeafileEntry entry)
            {
                if (entry.type == "back") { _navState.GoBack(); await LadeInhalt(); }
                else if (entry.type == "dir") { _navState.EnterFolder(entry.name); await LadeInhalt(); }
                else CtxDownload_Click(sender, e);
            }
        }

        private void LstRepos_DragEnter(object sender, DragEventArgs e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        protected override void OnShown(EventArgs e) { base.OnShown(e); this.Activate(); }
        private string FormatDate(long timestamp) => timestamp == 0 ? "-" : DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime().ToString("g");
        private void _lstDownloads_DoubleClick(object sender, EventArgs e) { if (_lstDownloads.SelectedItems.Count > 0) { if (_lstDownloads.SelectedItems[0].Tag is DownloadItem item) { new FrmTransferDetail(item).ShowDialog(); } } }
    }
}