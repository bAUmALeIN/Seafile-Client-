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
using System.Windows.Forms; // WICHTIG für TabPage
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class Form1 : MaterialForm
    {
        private SeafileClient _seafileClient;
        private NavigationState _navState;
        private readonly string _authToken;
        private DownloadManager _downloadManager;

        // UI Variablen
        private MaterialTabControl _tabControl;
        private MaterialTabSelector _tabSelector;

        // FIX: Explizit System.Windows.Forms.TabPage nutzen -> Keine Mehrdeutigkeit mehr!
        private System.Windows.Forms.TabPage _tabFiles;
        private System.Windows.Forms.TabPage _tabDownloads;

        private MaterialListView _lstDownloads;

        public Form1(string token)
        {
            InitializeComponent();
            _authToken = token;
            _navState = new NavigationState();
            _seafileClient = new SeafileClient(_authToken);

            _downloadManager = new DownloadManager(_seafileClient);
            _downloadManager.OnDownloadStarted += AddDownloadToUi;
            _downloadManager.OnItemUpdated += UpdateDownloadInUi;

            SetupMaterialSkin();
            InitializeCustomUI();
            InitializeTabs();
        }

        // =========================================================================
        // INITIALISIERUNG & UI SETUP
        // =========================================================================

        private void InitializeTabs()
        {
            // 1. Tab Selector
            _tabSelector = new MaterialTabSelector();
            _tabSelector.BaseTabControl = _tabControl;
            _tabSelector.Depth = 0;
            // Fettgedruckte Schrift für bessere Lesbarkeit
            _tabSelector.Font = new Font("Roboto", 14F, FontStyle.Bold, GraphicsUnit.Pixel);
            _tabSelector.Location = new Point(0, 64);
            _tabSelector.MouseState = MaterialDrawHelper.MaterialMouseState.HOVER;
            _tabSelector.Name = "tabSelector1";
            _tabSelector.Size = new Size(this.ClientSize.Width, 48);
            _tabSelector.TabIndex = 99;
            _tabSelector.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(_tabSelector);

            // 2. Tab Control
            _tabControl = new MaterialTabControl();
            _tabControl.Depth = 0;
            _tabControl.Location = new Point(0, 112);
            _tabControl.MouseState = MaterialDrawHelper.MaterialMouseState.HOVER;
            _tabControl.Name = "tabControl1";
            _tabControl.SelectedIndex = 0;
            _tabControl.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 112);
            _tabControl.TabIndex = 100;
            _tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // 3. Tab Seiten (System.Windows.Forms.TabPage)
            Color darkBackground = Color.FromArgb(50, 50, 50);

            _tabFiles = new System.Windows.Forms.TabPage();
            _tabFiles.Text = "Dateien";
            _tabFiles.BackColor = darkBackground;

            _tabDownloads = new System.Windows.Forms.TabPage();
            _tabDownloads.Text = "Downloads";
            _tabDownloads.BackColor = darkBackground;

            _tabControl.Controls.Add(_tabFiles);
            _tabControl.Controls.Add(_tabDownloads);

            _tabSelector.BaseTabControl = _tabControl;
            this.Controls.Add(_tabControl);

            // 4. Content Verschieben
            if (lstRepos != null && !lstRepos.IsDisposed)
            {
                if (this.Controls.Contains(lstRepos)) this.Controls.Remove(lstRepos);
                lstRepos.Parent = _tabFiles;
                lstRepos.Dock = DockStyle.Fill;
                lstRepos.Visible = true;
                lstRepos.BackColor = darkBackground;
            }

            if (Controls.ContainsKey("panelActionbar"))
            {
                var pnl = Controls["panelActionbar"];
                this.Controls.Remove(pnl);
                pnl.Parent = _tabFiles;
                pnl.Dock = DockStyle.Top;
                lstRepos.BringToFront();
            }

            // 5. Download Liste bauen - GENAU WIE LSTREPOS
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

            // Wir nutzen UiHelper.SetupListView für Icons und Font -> Gleicher Look!
            UiHelper.SetupListView(_lstDownloads);

            // Aber wir brauchen andere Spalten als die Repo-Liste:
            _lstDownloads.Columns.Clear();
            _lstDownloads.Columns.Add("Datei / Ordner", 300); // Index 0
            _lstDownloads.Columns.Add("Status", 250);         // Index 1 (wird breit gezogen)
            _lstDownloads.Columns.Add("Fortschritt", 120);    // Index 2
            _lstDownloads.Columns.Add("Startzeit", 100);      // Index 3

            // Events: Gleiche Taktik wie bei lstRepos für Resize und Layout
            _lstDownloads.SizeChanged += (s, e) => ResizeDownloadListColumns();
            _lstDownloads.ColumnWidthChanging += (s, e) => { e.Cancel = true; e.NewWidth = _lstDownloads.Columns[e.ColumnIndex].Width; };
        }

        // Die "Taktik" von UiHelper, aber angepasst für 4 Spalten (Downloads)
        private void ResizeDownloadListColumns()
        {
            if (_lstDownloads.Columns.Count < 4 || _lstDownloads.ClientSize.Width == 0) return;

            // Spalten 0, 2 und 3 haben feste Breiten. Spalte 1 (Status) soll den Rest füllen.
            int fixedWidth = _lstDownloads.Columns[0].Width + _lstDownloads.Columns[2].Width + _lstDownloads.Columns[3].Width;

            // Scrollbar checken wie im UiHelper
            bool hasScroll = false;
            try
            {
                if (_lstDownloads.Items.Count > 0)
                {
                    var rect = _lstDownloads.GetItemRect(0);
                    if ((_lstDownloads.Items.Count * rect.Height) > _lstDownloads.ClientSize.Height) hasScroll = true;
                }
            }
            catch { }

            int buffer = hasScroll ? SystemInformation.VerticalScrollBarWidth + 4 : 0;
            int availableWidth = _lstDownloads.ClientSize.Width - fixedWidth - buffer;

            if (availableWidth > 50) _lstDownloads.Columns[1].Width = availableWidth;
        }

        private void InitializeCustomUI()
        {
            ContextMenuStrip ctxMenu = MenuBuilder.CreateContextMenu(CtxDownload_Click, BtnDelete_Click);
            try
            {
                ctxMenu.Items[0].Image = MenuBuilder.ResizeIcon(Properties.Resources.icons8_dateidownload_40, 16, 16);
                ctxMenu.Items[2].Image = MenuBuilder.ResizeIcon(Properties.Resources.icons8_datei_löschen_40, 16, 16);
            }
            catch { }

            lstRepos.ContextMenuStrip = ctxMenu;
            UiHelper.SetupListView(lstRepos);

            lstRepos.DoubleClick += lstRepos_DoubleClick;
            lstRepos.SizeChanged += (s, e) => UiHelper.UpdateColumnWidths(lstRepos);
            lstRepos.ColumnWidthChanging += (s, e) => { e.Cancel = true; e.NewWidth = lstRepos.Columns[e.ColumnIndex].Width; };
            lstRepos.AllowDrop = true;
            lstRepos.DragEnter += LstRepos_DragEnter;
            lstRepos.DragDrop += LstRepos_DragDrop;

            materialButton2.Click += BtnNew_Click;
            materialButton3.Click += BtnDelete_Click;

            try { if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].BackColor = Color.FromArgb(45, 45, 48); } catch { }
        }

        private void SetupMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new MaterialColorScheme(
                MaterialPrimary.Orange500, MaterialPrimary.Orange700, MaterialPrimary.Orange200,
                MaterialAccent.DeepOrange400, MaterialTextShade.WHITE
            );
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "Lade Bibliotheken...";
            await LadeInhalt();
        }

        // =========================================================================
        // RESIZE FIX (WICHTIG: Auch für die Download-Liste aufrufen!)
        // =========================================================================
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lstRepos != null) UiHelper.UpdateColumnWidths(lstRepos);
            if (_lstDownloads != null) ResizeDownloadListColumns();
        }

        // =========================================================================
        // DOWNLOAD EVENT HANDLER
        // =========================================================================

        private void AddDownloadToUi(DownloadItem item)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<DownloadItem>(AddDownloadToUi), item); return; }

            var lvi = new ListViewItem(item.FileName);
            lvi.SubItems.Add(item.Status);
            lvi.SubItems.Add(item.Progress + "%");
            lvi.SubItems.Add(item.StartTime.ToShortTimeString());
            lvi.ImageKey = item.Type.Contains("Ordner") ? "dir" : "file"; // Icons aus SetupListView nutzen!
            item.Tag = lvi;
            _lstDownloads.Items.Add(lvi);

            ResizeDownloadListColumns(); // Sofort Layout fixen
        }

        private void UpdateDownloadInUi(DownloadItem item)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<DownloadItem>(UpdateDownloadInUi), item); return; }

            if (item.Tag is ListViewItem lvi)
            {
                lvi.SubItems[1].Text = item.Status;
                lvi.SubItems[2].Text = item.Progress + "%";

                if (item.Status == "Fertig")
                {
                    lvi.ForeColor = Color.LightGreen;
                }
                else if (item.Status.StartsWith("Fehler") || item.Status == "Abgebrochen")
                {
                    lvi.ForeColor = Color.Salmon;
                }
            }
        }

        // =========================================================================
        // DATEN LADEN
        // =========================================================================

        private async Task LadeInhalt()
        {
            try
            {
                if (_seafileClient == null) _seafileClient = new SeafileClient(_authToken);

                lstRepos.Items.Clear();
                lstRepos.Groups.Clear();
                lstRepos.ShowGroups = true;

                if (_navState.IsInRoot) await LoadLibraries();
                else await LoadDirectory(_navState.CurrentRepoId, _navState.CurrentPath);

                UiHelper.UpdateColumnWidths(lstRepos);
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Ladefehler", ex.Message); }
        }

        private async Task LoadLibraries()
        {
            ListViewGroup grpMine = new ListViewGroup("Meine Bibliotheken", HorizontalAlignment.Left);
            ListViewGroup grpShared = new ListViewGroup("Für mich freigegeben", HorizontalAlignment.Left);

            lstRepos.Groups.Add(grpMine);
            lstRepos.Groups.Add(grpShared);

            var repos = await _seafileClient.GetLibrariesAsync();

            foreach (var repo in repos)
            {
                var item = new ListViewItem("📚");
                item.SubItems.Add(repo.name);
                item.SubItems.Add((repo.size / 1024 / 1024) + " MB");
                item.SubItems.Add(FormatDate(repo.mtime));
                string ownerDisplay = repo.type == "grepo" ? "Gruppe" : (repo.owner ?? "-");
                item.SubItems.Add(ownerDisplay);
                item.Tag = repo;

                if (repo.type == "repo") item.Group = grpMine;
                else if (repo.type == "srepo") { item.Group = grpShared; item.SubItems[4].Text = repo.owner; }
                else if (repo.type == "grepo")
                {
                    string groupName = repo.owner;
                    ListViewGroup targetGroup = null;
                    foreach (ListViewGroup existingGroup in lstRepos.Groups) { if (existingGroup.Header == groupName) { targetGroup = existingGroup; break; } }
                    if (targetGroup == null) { targetGroup = new ListViewGroup(groupName, HorizontalAlignment.Left); lstRepos.Groups.Add(targetGroup); }
                    item.Group = targetGroup;
                }
                else item.Group = grpShared;

                lstRepos.Items.Add(item);
            }
            lblStatus.Text = $"Bereit. {repos.Count} Bibliotheken gefunden.";
        }

        private async Task LoadDirectory(string repoId, string path)
        {
            lstRepos.ShowGroups = false;
            var entries = await _seafileClient.GetDirectoryEntriesAsync(repoId, path);

            var backItem = new ListViewItem("🔙");
            backItem.SubItems.Add(".. [Zurück]");
            backItem.SubItems.Add(""); backItem.SubItems.Add(""); backItem.SubItems.Add("");
            backItem.Tag = new SeafileEntry { type = "back" };
            lstRepos.Items.Add(backItem);

            foreach (var entry in entries)
            {
                string icon = entry.type == "dir" ? "dir" : "file"; // Icons aus ImageList nutzen
                var item = new ListViewItem(icon); // Key statt Emoji
                item.SubItems.Add(entry.name);
                item.SubItems.Add(entry.type == "dir" ? "-" : (entry.size / 1024) + " KB");
                item.SubItems.Add(FormatDate(entry.mtime));
                item.SubItems.Add(entry.type);
                item.Tag = entry;
                lstRepos.Items.Add(item);
            }
            lblStatus.Text = $"{_navState.CurrentRepoName}: {_navState.CurrentPath}";
        }

        // =========================================================================
        // AKTIONEN
        // =========================================================================

        private async void CtxDownload_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
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

                    if (tag is Tuple<string, SeafileEntry> tuple) { repoId = tuple.Item1; entryToDownload = tuple.Item2; navPath = "/"; }
                    else if (tag is SeafileEntry entry) { entryToDownload = entry; }

                    if (entryToDownload != null)
                    {
                        _ = _downloadManager.DownloadEntryAsync(entryToDownload, repoId, navPath);
                        started = true;
                    }
                }

                if (started)
                {
                    MaterialSnackBar snack = new MaterialSnackBar("Download gestartet! Siehe Tab 'Downloads'", "OK", true);
                    snack.Show(this);
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
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
                        if (success) { await LadeInhalt(); UiHelper.ShowSuccessDialog("Erfolg", "Bibliothek erstellt."); }
                    }
                }
                else
                {
                    string folderName = UiHelper.ShowInputDialog("Neuer Ordner", "Name:");
                    if (!string.IsNullOrWhiteSpace(folderName))
                    {
                        string newPath = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + folderName : _navState.CurrentPath + "/" + folderName;
                        bool success = await _seafileClient.CreateDirectoryAsync(_navState.CurrentRepoId, newPath);
                        if (success) await LadeInhalt();
                    }
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            var tag = lstRepos.SelectedItems[0].Tag;

            try
            {
                if (tag is SeafileEntry entry && entry.type != "back")
                {
                    if (UiHelper.ShowDangerConfirmation("Datei löschen", $"Wirklich '{entry.name}' löschen?"))
                    {
                        string path = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + entry.name : _navState.CurrentPath + "/" + entry.name;
                        bool success = await _seafileClient.DeleteEntryAsync(_navState.CurrentRepoId, path, entry.type == "dir");
                        if (success) { await LadeInhalt(); UiHelper.ShowInfoDialog("Gelöscht", "Eintrag wurde entfernt."); }
                    }
                }
                else if (tag is SeafileRepo repo)
                {
                    if (UiHelper.ShowDangerConfirmation("BIBLIOTHEK LÖSCHEN", $"ACHTUNG: '{repo.name}' wirklich unwiderruflich löschen?"))
                    {
                        bool success = await _seafileClient.DeleteLibraryAsync(repo.id);
                        if (success) { await LadeInhalt(); UiHelper.ShowInfoDialog("Gelöscht", "Bibliothek entfernt."); }
                    }
                }
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Löschen fehlgeschlagen", ex.Message); }
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

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = UiHelper.ShowInputDialog("Globale Suche", "Suchbegriff:");
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await LadeInhalt();
                return;
            }

            _tabControl.SelectedIndex = 0;
            if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].Enabled = false;

            lstRepos.Visible = false;
            lstRepos.Items.Clear();
            lstRepos.Groups.Clear();
            lstRepos.ShowGroups = true;

            string termLower = searchTerm.ToLower();
            lblStatus.Text = "Suche läuft... (Kann dauern)";
            Application.DoEvents();

            try
            {
                var allRepos = await _seafileClient.GetLibrariesAsync();
                int totalHits = 0;
                int errorCount = 0;

                using (var semaphore = new System.Threading.SemaphoreSlim(3))
                {
                    var searchTasks = allRepos.Select(async repo =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var entries = await _seafileClient.GetAllFilesRecursiveAsync(repo.id);
                            return new { Repo = repo, Entries = entries };
                        }
                        catch { errorCount++; return null; }
                        finally { semaphore.Release(); }
                    });

                    var results = await Task.WhenAll(searchTasks);
                    lstRepos.BeginUpdate();

                    foreach (var result in results)
                    {
                        if (result == null || result.Entries == null) continue;
                        var matches = result.Entries.Where(entry => (entry.name != null && entry.name.ToLower().Contains(termLower)) || (entry.parent_dir != null && entry.parent_dir.ToLower().Contains(termLower))).ToList();

                        if (matches.Count > 0)
                        {
                            string groupTitle = result.Repo.type == "grepo" ? result.Repo.owner : result.Repo.name;
                            if (result.Repo.type == "srepo") groupTitle += $" (von {result.Repo.owner})";
                            ListViewGroup repoGroup = new ListViewGroup(groupTitle, HorizontalAlignment.Left);
                            lstRepos.Groups.Add(repoGroup);

                            foreach (var entry in matches)
                            {
                                string folder = string.IsNullOrEmpty(entry.parent_dir) ? "/" : entry.parent_dir;
                                if (!folder.EndsWith("/")) folder += "/";
                                string displayName = folder + entry.name;
                                string icon = entry.type == "dir" ? "dir" : "file";
                                var item = new ListViewItem(icon);
                                item.SubItems.Add(displayName);
                                item.SubItems.Add(entry.type == "dir" ? "-" : (entry.size / 1024) + " KB");
                                item.SubItems.Add(FormatDate(entry.mtime));
                                item.SubItems.Add(entry.type);
                                item.Tag = new Tuple<string, SeafileEntry>(result.Repo.id, entry);
                                item.Group = repoGroup;
                                lstRepos.Items.Add(item);
                                totalHits++;
                            }
                        }
                    }
                    lstRepos.EndUpdate();
                }
                string statusMsg = $"Gefunden: {totalHits} Treffer.";
                if (errorCount > 0) statusMsg += $" ({errorCount} Fehler)";
                lblStatus.Text = statusMsg;
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); await LadeInhalt(); }
            finally { lstRepos.Visible = true; if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].Enabled = true; UiHelper.UpdateColumnWidths(lstRepos); }
        }

        private async void LstRepos_DragDrop(object sender, DragEventArgs e)
        {
            if (_navState.IsInRoot) { UiHelper.ShowInfoDialog("Info", "Bitte öffne erst eine Bibliothek für den Upload."); return; }
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (droppedPaths != null && droppedPaths.Length > 0)
            {
                try
                {
                    foreach (string path in droppedPaths) await ProcessDropEntryRecursive(path, _navState.CurrentPath);
                    await LadeInhalt();
                    UiHelper.ShowSuccessDialog("Upload fertig", "Alle Dateien wurden hochgeladen.");
                }
                catch (Exception ex) { UiHelper.ShowErrorDialog("Upload Fehler", ex.Message); }
            }
        }

        private async Task ProcessDropEntryRecursive(string localPath, string remoteTargetFolder)
        {
            FileAttributes attr = File.GetAttributes(localPath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string folderName = new DirectoryInfo(localPath).Name;
                string newRemotePath = remoteTargetFolder.EndsWith("/") ? remoteTargetFolder + folderName : remoteTargetFolder + "/" + folderName;
                lblStatus.Text = $"Erstelle Ordner: {folderName}";
                await _seafileClient.CreateDirectoryAsync(_navState.CurrentRepoId, newRemotePath);
                foreach (string entry in Directory.GetFileSystemEntries(localPath)) await ProcessDropEntryRecursive(entry, newRemotePath);
            }
            else
            {
                string fileName = Path.GetFileName(localPath);
                string link = await _seafileClient.GetUploadLinkAsync(_navState.CurrentRepoId, remoteTargetFolder);
                if (!string.IsNullOrEmpty(link)) { lblStatus.Text = $"Upload: {fileName}"; await _seafileClient.UploadFileAsync(link, localPath, remoteTargetFolder, fileName); }
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
    }
}