using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Helper;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Controls;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class Form1 : MaterialForm
    {
        // Core Services
        private SeafileClient _seafileClient;
        private NavigationState _navState;
        private readonly string _authToken;
        private DownloadManager _downloadManager;
        private BreadcrumbManager _breadcrumbManager;
        private CacheManager _cacheManager;
        private CancellationTokenSource _thumbnailCts;

        // UI Controls
        private MaterialTabControl _tabControl;
        private MaterialTabSelector _tabSelector;
        private System.Windows.Forms.TabPage _tabFiles;
        private System.Windows.Forms.TabPage _tabDownloads;
        private MaterialListView _lstDownloads;
        private FlowLayoutPanel _flowPath;
        private ImageList _repoIcons;
        private PictureBox _appIcon;

        // Auto-Refresh Logic
        private System.Windows.Forms.Timer _refreshDebounceTimer;

        public Form1(string token)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();

            // Settings laden
            LoadSettingsFromDb();

            _authToken = token;
            _navState = new NavigationState();
            _cacheManager = new CacheManager();
            _seafileClient = new SeafileClient(_authToken);

            // Timer Setup
            _refreshDebounceTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _refreshDebounceTimer.Tick += async (s, e) =>
            {
                _refreshDebounceTimer.Stop();
                if (_tabControl.SelectedTab == _tabFiles)
                {
                    _cacheManager.Clear();
                    await LadeInhalt();
                }
            };

            _downloadManager = new DownloadManager(_seafileClient);
            _downloadManager.OnDownloadStarted += AddDownloadToUi;
            _downloadManager.OnItemUpdated += UpdateDownloadInUi;

            // UI Setup
            SetupMaterialSkin();
            InitializeIcons();

            // Logo initialisieren (wird später platziert)
            InitializeLogoResource();

            // Tabs initialisieren
            InitializeTabs();

            // Custom UI (Hier wird das Logo platziert und die Buttons erstellt)
            InitializeCustomUI();

            // Breadcrumbs NACH der Custom UI initialisieren, damit wir die Position der Buttons kennen
            InitializeBreadcrumbsWrapper();
        }

        private void LoadSettingsFromDb()
        {
            var db = new DBHelper();
            string url = db.GetSetting(AppConfig.SettingsKeys.ApiUrl);
            string login = db.GetSetting(AppConfig.SettingsKeys.LoginUrl);

            if (!string.IsNullOrEmpty(url)) AppConfig.ApiBaseUrl = url;
            if (!string.IsNullOrEmpty(login)) AppConfig.LoginUrl = login;
        }

        // =========================================================================
        // SETUP & INITIALISIERUNG
        // =========================================================================

        private void SetupMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new MaterialColorScheme(
                MaterialPrimary.BlueGrey800, MaterialPrimary.BlueGrey900, MaterialPrimary.BlueGrey500,
                MaterialAccent.Orange400, MaterialTextShade.WHITE
            );
        }

        private void InitializeIcons()
        {
            _repoIcons = new ImageList { ImageSize = new Size(24, 24), ColorDepth = ColorDepth.Depth32Bit };
            _repoIcons.Images.Add("dir", Properties.Resources.icon_folder);
            _repoIcons.Images.Add("file", Properties.Resources.icon_file);
            _repoIcons.Images.Add("repo", Properties.Resources.icon_repo);
            _repoIcons.Images.Add("back", Properties.Resources.icon_back);
        }

        private void InitializeLogoResource()
        {
            _appIcon = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Size = new Size(40, 40),
                Cursor = Cursors.Default
            };

            try { _appIcon.Image = Properties.Resources.app_logo; }
            catch { _appIcon.Image = Properties.Resources.icon_repo; }

            this.Text = "BBS-ME File Explorer";
        }

        private void InitializeTabs()
        {
            Color darkBackground = Color.FromArgb(50, 50, 50);

            _tabControl = new MaterialTabControl
            {
                Depth = 0,
                Location = new Point(0, 112),
                MouseState = MaterialDrawHelper.MaterialMouseState.HOVER,
                Name = "tabControl1",
                SelectedIndex = 0,
                Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 112),
                TabIndex = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _tabFiles = new System.Windows.Forms.TabPage { Text = "Dateien", BackColor = darkBackground };
            _tabDownloads = new System.Windows.Forms.TabPage { Text = "Transfers", BackColor = darkBackground };

            _tabControl.Controls.Add(_tabFiles);
            _tabControl.Controls.Add(_tabDownloads);
            this.Controls.Add(_tabControl);

            _tabSelector = new MaterialTabSelector
            {
                BaseTabControl = _tabControl,
                Depth = 0,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point(0, 64),
                MouseState = MaterialDrawHelper.MaterialMouseState.HOVER,
                Name = "tabSelector1",
                Size = new Size(this.ClientSize.Width, 48),
                TabIndex = 99,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                CharacterCasing = MaterialTabSelector.CustomCharacterCasing.Normal
            };
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
            }

            SetupDownloadListView(darkBackground);
        }

        private void SetupDownloadListView(Color background)
        {
            _lstDownloads = new MaterialListView
            {
                Parent = _tabDownloads,
                Dock = DockStyle.Fill,
                Depth = 0,
                MouseState = MaterialDrawHelper.MaterialMouseState.HOVER,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                OwnerDraw = true,
                View = View.Details,
                BackColor = background
            };

            _lstDownloads.Columns.Add("Datei / Ordner", 400);
            _lstDownloads.Columns.Add("Status", 200);
            _lstDownloads.Columns.Add("Fortschritt", 100);
            _lstDownloads.Columns.Add("Startzeit", 120);

            UiHelper.SetupListView(_lstDownloads);

            ImageList downloadIcons = new ImageList { ImageSize = new Size(24, 24), ColorDepth = ColorDepth.Depth32Bit };
            downloadIcons.Images.Add("upload", Properties.Resources.icon_upload);
            downloadIcons.Images.Add("download", Properties.Resources.icon_download);
            _lstDownloads.SmallImageList = downloadIcons;

            _lstDownloads.SizeChanged += (s, e) => UiHelper.UpdateColumnWidths(_lstDownloads);
            _lstDownloads.ColumnWidthChanging += (s, e) => { e.Cancel = true; e.NewWidth = _lstDownloads.Columns[e.ColumnIndex].Width; };
            _lstDownloads.DoubleClick += _lstDownloads_DoubleClick;
        }

        private void InitializeBreadcrumbsWrapper()
        {
            Control actionPanel = null;
            if (_tabFiles.Controls.ContainsKey("panelActionbar")) actionPanel = _tabFiles.Controls["panelActionbar"];

            if (actionPanel == null) return;

            // FIX: Dynamische Berechnung der Startposition basierend auf dem 'Löschen'-Button
            int startX = 280; // Fallback
            if (actionPanel.Controls.ContainsKey("btnDelete"))
            {
                var btnDel = actionPanel.Controls["btnDelete"];
                // 25px Puffer nach dem Button, damit das Home Icon nicht überlappt wird
                startX = btnDel.Right + 25;
            }

            _flowPath = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(startX, 15)
            };
            actionPanel.Controls.Add(_flowPath);
            _flowPath.BringToFront(); // Sicherstellen, dass es über allem anderen liegt

            _breadcrumbManager = new BreadcrumbManager(
                _flowPath,
                _navState,
                async () => { _navState.ResetToRoot(); await LadeInhalt(); },
                async (id, name) => { _navState.EnterRepo(id, name); await LadeInhalt(); },
                null
            );
        }

        private void UpdateBreadcrumbs(string searchContext = null)
        {
            _breadcrumbManager?.Update(searchContext);
        }

        private void InitializeCustomUI()
        {
            ContextMenuStrip ctxMenu = MenuBuilder.CreateContextMenu(CtxDownload_Click, BtnDelete_Click);
            ToolStripMenuItem itemJump = new ToolStripMenuItem("Gehe zu");
            itemJump.Name = "ItemJump";
            itemJump.Click += CtxJumpTo_Click;
            itemJump.Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_jump, 16, 16);

            ctxMenu.Items.Insert(0, new ToolStripSeparator());
            ctxMenu.Items.Insert(0, itemJump);

            if (ctxMenu.Items.Count > 2 && ctxMenu.Items[2] != null) ctxMenu.Items[2].Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_download, 16, 16);
            if (ctxMenu.Items.Count > 4 && ctxMenu.Items[4] != null) ctxMenu.Items[4].Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_löschen, 16, 16);

            ctxMenu.Opening += CtxMenu_Opening;

            lstRepos.ContextMenuStrip = ctxMenu;
            UiHelper.SetupListView(lstRepos, _repoIcons);

            lstRepos.Columns.Clear();
            lstRepos.Columns.Add("Name", 400);
            lstRepos.Columns.Add("Größe", 90);
            lstRepos.Columns.Add("Geändert", 160);
            lstRepos.Columns.Add("Typ / Pfad", 150);

            lstRepos.DoubleClick += lstRepos_DoubleClick;
            lstRepos.SizeChanged += (s, e) => UiHelper.UpdateColumnWidths(lstRepos);

            lstRepos.AllowDrop = true;
            lstRepos.DragEnter += LstRepos_DragEnter;
            lstRepos.DragDrop += LstRepos_DragDrop;

            ReplaceMaterialButtonsWithStandard();

            try { if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].BackColor = Color.FromArgb(45, 45, 48); } catch { }
        }

        private void CtxMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is ContextMenuStrip cms && lstRepos.SelectedItems.Count > 0)
            {
                var firstTag = lstRepos.SelectedItems[0].Tag;
                ToolStripItem itemJump = cms.Items["ItemJump"];
                if (itemJump != null)
                {
                    itemJump.Visible = firstTag is Tuple<string, SeafileEntry>;
                }
            }
            else if (lstRepos.SelectedItems.Count == 0)
            {
                e.Cancel = true;
            }
        }

        private void ReplaceMaterialButtonsWithStandard()
        {
            Control actionPanel = null;
            if (Controls.ContainsKey("panelActionbar")) actionPanel = Controls["panelActionbar"];
            else if (_tabFiles.Controls.ContainsKey("panelActionbar")) actionPanel = _tabFiles.Controls["panelActionbar"];

            if (actionPanel == null) return;

            // Entferne alte Buttons
            var toRemove = actionPanel.Controls.OfType<Control>()
                .Where(c => c is MaterialButton || c.Name.StartsWith("materialButton") || c.Name == "btnLogout" || c.Name == "btnSettings" || c.Name == "btnSearch" || c is System.Windows.Forms.Button)
                .ToList();
            foreach (var c in toRemove) actionPanel.Controls.Remove(c);

            // FIX: Hintergrundfarbe des Panels explizit setzen
            actionPanel.BackColor = Color.FromArgb(45, 45, 48);

            // 0. LOGO EINFÜGEN (Ganz Links)
            if (_appIcon != null)
            {
                _appIcon.Parent = actionPanel;
                _appIcon.Location = new Point(10, (actionPanel.Height - _appIcon.Height) / 2);
                if (!actionPanel.Controls.Contains(_appIcon))
                    actionPanel.Controls.Add(_appIcon);
                _appIcon.BringToFront();
            }

            // Startposition für LINKE Buttons berechnen (Rechts vom Logo)
            int leftX = (_appIcon != null) ? _appIcon.Right + 15 : 10;
            int btnY = 12;

            // --- RECHTE SEITE (Von Rechts nach Links aufbauen) ---
            int rightEdge = actionPanel.Width - 10;

            // 1. SETTINGS BUTTON (Ganz rechts, jetzt mit Text "EINSTELLUNGEN")
            System.Windows.Forms.Button btnSettings = CreateFlatButton("EINSTELLUNGEN", Properties.Resources.icon_settings);
            btnSettings.Name = "btnSettings";
            // Position berechnen: Rechter Rand - Button Breite
            btnSettings.Location = new Point(rightEdge - btnSettings.Width, btnY);
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Click += (s, e) => new FrmSettings().ShowDialog();
            actionPanel.Controls.Add(btnSettings);

            // 2. LOGOUT BUTTON (Links neben Settings)
            System.Windows.Forms.Button btnOut = CreateFlatButton("AUSLOGGEN", Properties.Resources.icon_logout);
            btnOut.Name = "btnLogout";
            // Position: Settings.Left - 10px Gap - Eigene Breite
            btnOut.Location = new Point(btnSettings.Left - 10 - btnOut.Width, btnY);
            btnOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOut.Click += btnLogout_Click;
            actionPanel.Controls.Add(btnOut);

            // 3. SEARCH BUTTON (Links neben Logout)
            System.Windows.Forms.Button btnSearch = CreateFlatButton("SUCHEN", Properties.Resources.icon_search);
            btnSearch.Name = "btnSearch";
            btnSearch.Location = new Point(btnOut.Left - 10 - btnSearch.Width, btnY);
            btnSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSearch.Click += btnSearch_Click;
            actionPanel.Controls.Add(btnSearch);


            // --- LINKE SEITE (Von Links nach Rechts) ---

            // 4. NEU BUTTON
            System.Windows.Forms.Button btnNew = CreateFlatButton("NEU", Properties.Resources.icon_new);
            btnNew.Name = "btnNew";
            btnNew.Location = new Point(leftX, btnY);
            btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnNew.Click += BtnNew_Click;
            actionPanel.Controls.Add(btnNew);

            // 5. LÖSCHEN BUTTON
            System.Windows.Forms.Button btnDel = CreateFlatButton("LÖSCHEN", Properties.Resources.icon_delete);
            btnDel.Name = "btnDelete"; // WICHTIG für Breadcrumb Referenz
            btnDel.Location = new Point(btnNew.Right + 10, btnY);
            btnDel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnDel.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDel);
        }

        private System.Windows.Forms.Button CreateFlatButton(string text, Image icon)
        {
            System.Windows.Forms.Button btn = new StableButton();
            btn.Text = " " + text;
            btn.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btn.ForeColor = Color.White;
            btn.BackColor = Color.Transparent;
            if (icon != null) btn.Image = ResizeImage(icon, 24, 24);
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
            // Breite: Icon(24) + Gap(10) + Text + Gap(10)
            btn.Width = 24 + 10 + textSize.Width + 10;
            return btn;
        }

        private Image ResizeImage(Image img, int w, int h) => new Bitmap(img, new Size(w, h));

        // =========================================================================
        // LOGIC MIT CACHE & THUMBNAILS
        // =========================================================================

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

        private void ResizeDownloadListColumns()
        {
            if (_lstDownloads.Columns.Count < 4 || _lstDownloads.ClientSize.Width == 0) return;
            int fixedWidth = _lstDownloads.Columns[0].Width + _lstDownloads.Columns[2].Width + _lstDownloads.Columns[3].Width;
            int availableWidth = _lstDownloads.ClientSize.Width - fixedWidth - 20;
            if (availableWidth > 50) _lstDownloads.Columns[1].Width = availableWidth;
        }

        private async Task LadeInhalt()
        {
            try
            {
                _thumbnailCts?.Cancel();
                _thumbnailCts = new CancellationTokenSource();

                UpdateBreadcrumbs();
                if (_seafileClient == null) _seafileClient = new SeafileClient(_authToken);
                lstRepos.Items.Clear();
                lstRepos.Groups.Clear();
                lstRepos.ShowGroups = true;

                string cacheKey = _navState.IsInRoot ? "root" : $"{_navState.CurrentRepoId}:{_navState.CurrentPath}";
                var cachedEntries = _cacheManager.Get<List<object>>(cacheKey);

                if (cachedEntries != null)
                {
                    RenderItems(cachedEntries);
                }
                else
                {
                    List<object> fetchedItems = new List<object>();

                    if (_navState.IsInRoot)
                    {
                        var repos = await _seafileClient.GetLibrariesAsync();
                        fetchedItems.AddRange(repos);
                        await LoadLibrariesUI(repos);
                    }
                    else
                    {
                        var entries = await _seafileClient.GetDirectoryEntriesAsync(_navState.CurrentRepoId, _navState.CurrentPath);
                        fetchedItems.AddRange(entries);
                        LoadDirectoryUI(entries);
                    }
                    _cacheManager.Set(cacheKey, fetchedItems);
                }

                UiHelper.UpdateColumnWidths(lstRepos);

                if (!_navState.IsInRoot)
                {
                    _ = LoadThumbnailsAsync(_navState.CurrentRepoId, _navState.CurrentPath, _thumbnailCts.Token);
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Ladefehler", ex.Message); }
        }

        private void RenderItems(List<object> items)
        {
            if (_navState.IsInRoot)
            {
                var repos = items.Cast<SeafileRepo>().ToList();
                _ = LoadLibrariesUI(repos);
            }
            else
            {
                var entries = items.Cast<SeafileEntry>().ToList();
                LoadDirectoryUI(entries);
            }
        }

        private async Task LoadLibrariesUI(List<SeafileRepo> repos)
        {
            ListViewGroup grpMine = new ListViewGroup("   MEINE BIBLIOTHEKEN", HorizontalAlignment.Left);
            ListViewGroup grpShared = new ListViewGroup("   FÜR MICH FREIGEGEBEN", HorizontalAlignment.Left);
            lstRepos.Groups.Add(grpMine);
            lstRepos.Groups.Add(grpShared);

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
                        if (existingGroup.Header == groupName) { targetGroup = existingGroup; break; }
                    if (targetGroup == null) { targetGroup = new ListViewGroup(groupName, HorizontalAlignment.Left); lstRepos.Groups.Add(targetGroup); }
                    item.Group = targetGroup;
                }
                else item.Group = grpShared;
                lstRepos.Items.Add(item);
            }
        }

        private void LoadDirectoryUI(List<SeafileEntry> entries)
        {
            lstRepos.ShowGroups = false;
            var backItem = new ListViewItem(".. [Zurück]", "back");
            backItem.Tag = new SeafileEntry { type = "back" };
            lstRepos.Items.Add(backItem);

            foreach (var entry in entries)
            {
                string iconKey = "file";
                if (entry.type == "dir") iconKey = "dir";
                else
                {
                    string ext = System.IO.Path.GetExtension(entry.name).ToLower();
                    if (!_repoIcons.Images.ContainsKey(ext))
                    {
                        Icon sysIcon = IconHelper.GetIconForExtension(ext, false);
                        if (sysIcon != null) _repoIcons.Images.Add(ext, sysIcon);
                        else _repoIcons.Images.Add(ext, Properties.Resources.icon_file);
                    }
                    iconKey = ext;
                }

                var item = new ListViewItem(entry.name, iconKey);
                item.SubItems.Add(entry.type == "dir" ? "-" : (entry.size / 1024) + " KB");
                item.SubItems.Add(FormatDate(entry.mtime));
                item.SubItems.Add(entry.type);
                item.Tag = entry;
                lstRepos.Items.Add(item);
            }
            lblStatus.Text = $"{_navState.CurrentRepoName}: {_navState.CurrentPath}";
        }

        private async Task LoadThumbnailsAsync(string repoId, string path, CancellationToken token)
        {
            List<ListViewItem> itemsToCheck = new List<ListViewItem>();
            foreach (ListViewItem item in lstRepos.Items) itemsToCheck.Add(item);

            foreach (var item in itemsToCheck)
            {
                if (token.IsCancellationRequested) return;
                if (!(item.Tag is SeafileEntry entry) || entry.type == "dir" || entry.type == "back") continue;

                string ext = System.IO.Path.GetExtension(entry.name).ToLower();
                if (ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".gif")
                {
                    string fullPath = path.EndsWith("/") ? path + entry.name : path + "/" + entry.name;
                    Image thumb = await _seafileClient.GetThumbnailAsync(repoId, fullPath, 48);

                    if (thumb != null && !token.IsCancellationRequested)
                    {
                        this.Invoke(new Action(() =>
                        {
                            string key = "thumb_" + entry.id;
                            if (!_repoIcons.Images.ContainsKey(key))
                            {
                                _repoIcons.Images.Add(key, thumb);
                            }
                            if (item.ListView != null)
                            {
                                item.ImageKey = key;
                            }
                        }));
                    }
                }
            }
        }

        private async void BtnNew_Click(object sender, EventArgs e)
        {
            try
            {
                if (_navState.IsInRoot)
                {
                    string libName = UiHelper.ShowInputDialog("Neue Bibliothek", "Name:");
                    if (!string.IsNullOrWhiteSpace(libName))
                    {
                        bool success = await _seafileClient.CreateLibraryAsync(libName);
                        if (success) { _cacheManager.Clear(); await LadeInhalt(); UiHelper.ShowSuccessDialog("Erfolg", "Bibliothek erstellt."); }
                    }
                }
                else
                {
                    string folderName = UiHelper.ShowInputDialog("Neuer Ordner", "Name:");
                    if (!string.IsNullOrWhiteSpace(folderName))
                    {
                        string newPath = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + folderName : _navState.CurrentPath + "/" + folderName;
                        bool success = await _seafileClient.CreateDirectoryAsync(_navState.CurrentRepoId, newPath);
                        if (success) { _cacheManager.Clear(); await LadeInhalt(); }
                    }
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;

            int count = lstRepos.SelectedItems.Count;
            string message = count == 1
                ? GetDeleteMessage(lstRepos.SelectedItems[0].Tag)
                : $"Möchten Sie wirklich {count} Elemente unwiderruflich löschen?";

            if (!UiHelper.ShowDangerConfirmation("LÖSCHEN BESTÄTIGEN", message)) return;

            int successCount = 0;
            try
            {
                foreach (ListViewItem item in lstRepos.SelectedItems)
                {
                    if (await DeleteItem(item.Tag)) successCount++;
                }

                if (successCount > 0)
                {
                    _cacheManager.Clear();
                    await LadeInhalt();
                    new MaterialSnackBar($"{successCount} Elemente gelöscht.", "OK", true).Show(this);
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Löschen teilweise fehlgeschlagen", ex.Message); }
        }

        private string GetDeleteMessage(object tag)
        {
            if (tag is SeafileEntry entry) return entry.type == "back" ? "" : $"Möchten Sie '{entry.name}' wirklich löschen?";
            if (tag is SeafileRepo repo) return $"Möchten Sie die Bibliothek '{repo.name}' wirklich löschen?";
            return "Löschen?";
        }

        private async Task<bool> DeleteItem(object tag)
        {
            if (tag is SeafileEntry entry && entry.type != "back")
            {
                bool isDir = entry.type == "dir";
                string path = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + entry.name : _navState.CurrentPath + "/" + entry.name;
                return await _seafileClient.DeleteEntryAsync(_navState.CurrentRepoId, path, isDir);
            }
            if (tag is SeafileRepo repo)
            {
                return await _seafileClient.DeleteLibraryAsync(repo.id);
            }
            return false;
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = UiHelper.ShowInputDialog("Globale Suche", "Suchbegriff:");
            if (string.IsNullOrWhiteSpace(searchTerm)) { await LadeInhalt(); return; }

            lstRepos.Visible = false;
            lstRepos.Items.Clear();
            lstRepos.Groups.Clear();
            lstRepos.ShowGroups = true;

            try
            {
                lblStatus.Text = "Suche läuft...";
                UpdateBreadcrumbs("Suche: " + searchTerm);

                var allRepos = await _seafileClient.GetLibrariesAsync();

                var searchTasks = new List<Task>();
                var searchResults = new ConcurrentBag<ListViewItem>();
                var groups = new ConcurrentDictionary<string, ListViewGroup>();

                foreach (var repo in allRepos)
                {
                    searchTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var entries = await _seafileClient.GetAllFilesRecursiveAsync(repo.id);
                            var matches = entries.Where(x => x.name.ToLower().Contains(searchTerm.ToLower())).ToList();

                            if (matches.Count > 0)
                            {
                                var grp = new ListViewGroup(repo.name, HorizontalAlignment.Left);
                                groups.TryAdd(repo.id, grp);

                                foreach (var entry in matches)
                                {
                                    string path = (entry.parent_dir ?? "/") + entry.name;
                                    path = path.Replace("//", "/");
                                    string displayText = $"[{repo.name}] {path}";

                                    var item = new ListViewItem(displayText, entry.type == "dir" ? "dir" : "file");
                                    item.SubItems.Add(entry.type == "dir" ? "-" : UiHelper.FormatByteSize(entry.size));
                                    item.SubItems.Add(FormatDate(entry.mtime));
                                    item.SubItems.Add(entry.type);

                                    item.Tag = new Tuple<string, SeafileEntry>(repo.id, entry);
                                    item.Group = grp;

                                    searchResults.Add(item);
                                }
                            }
                        }
                        catch { }
                    }));
                }

                await Task.WhenAll(searchTasks);

                lstRepos.BeginUpdate();
                lstRepos.Groups.AddRange(groups.Values.OrderBy(g => g.Header).ToArray());
                lstRepos.Items.AddRange(searchResults.ToArray());

            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
            finally
            {
                lstRepos.EndUpdate();
                lstRepos.Visible = true;
                lblStatus.Text = "Suche beendet.";
                UiHelper.UpdateColumnWidths(lstRepos);
            }
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

                if (string.IsNullOrWhiteSpace(targetPath)) targetPath = "/";
                targetPath = targetPath.Replace("\\", "/");
                if (!targetPath.StartsWith("/")) targetPath = "/" + targetPath;
                targetPath = targetPath.Replace("//", "/");

                _navState.EnterRepo(repoId, "...");

                if (targetPath != "/") _navState.SetPath(targetPath);

                if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].Enabled = true;
                lstRepos.Visible = true;

                await LadeInhalt();

                foreach (ListViewItem item in lstRepos.Items)
                {
                    if (item.Tag is SeafileEntry dirEntry && dirEntry.name == entry.name)
                    {
                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }
            else
            {
                UiHelper.ShowInfoDialog("Info", "Du bist bereits in diesem Verzeichnis oder Funktion hier nicht verfügbar.");
            }
        }

        private void CtxDownload_Click(object sender, EventArgs e)
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
                try { HandleSingleDownload(tag); }
                catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
            }
        }

        private void HandleSingleDownload(object tag)
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

            if (started) new MaterialSnackBar("Download gestartet!", "OK", true).Show(this);
        }

        private async void lstRepos_DoubleClick(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            var tag = lstRepos.SelectedItems[0].Tag;

            if (tag is SeafileRepo repo)
            {
                _navState.EnterRepo(repo.id, repo.name);
                await LadeInhalt();
            }
            else if (tag is SeafileEntry entry)
            {
                if (entry.type == "back") { _navState.GoBack(); await LadeInhalt(); }
                else if (entry.type == "dir") { _navState.EnterFolder(entry.name); await LadeInhalt(); }
                else CtxDownload_Click(sender, e);
            }
        }

        private void LstRepos_DragEnter(object sender, DragEventArgs e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        private void LstRepos_DragDrop(object sender, DragEventArgs e)
        {
            if (_navState.IsInRoot) { UiHelper.ShowInfoDialog("Info", "Bitte öffne erst eine Bibliothek."); return; }
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (droppedPaths != null && droppedPaths.Length > 0)
            {
                try
                {
                    _ = _downloadManager.UploadFilesAsync(droppedPaths, _navState.CurrentRepoId, _navState.CurrentPath);
                    new MaterialSnackBar("Upload gestartet!", "OK", true).Show(this);
                }
                catch (Exception ex) { UiHelper.ShowScrollableErrorDialog("Upload Fehler", ex.Message); }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (UiHelper.ShowConfirmationDialog("Abmelden", "Möchtest du dich wirklich abmelden?"))
            {
                new DBHelper().DeleteToken();
                new AuthManager(null).ClearBrowserCacheOnDisk();
                Application.Restart();
            }
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

            if (item.Status == "Fertig" && item.Type == "Upload" && _refreshDebounceTimer != null)
            {
                _refreshDebounceTimer.Stop();
                _refreshDebounceTimer.Start();
            }
        }

        private void _lstDownloads_DoubleClick(object sender, EventArgs e)
        {
            if (_lstDownloads.SelectedItems.Count > 0 && _lstDownloads.SelectedItems[0].Tag is DownloadItem item)
                new FrmTransferDetail(item).ShowDialog();
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); this.Activate(); }
        private string FormatDate(long timestamp) => timestamp == 0 ? "-" : DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime().ToString("g");
    }
}