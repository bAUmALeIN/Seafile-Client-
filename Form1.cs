using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;
using System.IO;

namespace WinFormsApp3
{
    public partial class Form1 : MaterialForm
    {
        private SeafileClient _seafileClient;
        private NavigationState _navState;
        private readonly string _authToken;
        private DownloadManager _downloadManager;
        private BreadcrumbManager _breadcrumbManager;
        private CacheManager _cacheManager;
        private CancellationTokenSource _thumbnailCts;
        private System.Windows.Forms.Timer _refreshDebounceTimer;
        private Cursor _dragCursor = null;

        public Form1(string token)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();
            _authToken = token;
            LoadSettingsFromDb();
            InitializeCoreServices();
            InitializeUiComponents();
            SetupRefreshTimer();
        }

        private void InitializeCoreServices()
        {
            _navState = new NavigationState();
            _cacheManager = new CacheManager();
            _seafileClient = new SeafileClient(_authToken);
            _downloadManager = new DownloadManager(_seafileClient);

            // UI Events verbinden
            _downloadManager.OnDownloadStarted += AddDownloadToUi;
            _downloadManager.OnItemUpdated += UpdateDownloadInUi;

            // NEU: Wenn ein Transfer fertig ist (finally block), Timer für Refresh starten
            _downloadManager.OnTransferFinished += () =>
            {
                // Wir nutzen Invoke, da das Event aus einem Background-Thread kommen kann
                this.Invoke(new Action(() =>
                {
                    // Timer neustarten (Debounce), damit wir nicht spammen
                    _refreshDebounceTimer.Stop();
                    _refreshDebounceTimer.Start();
                }));
            };
        }

        private void SetupRefreshTimer()
        {
            _refreshDebounceTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _refreshDebounceTimer.Tick += async (s, e) =>
            {
                _refreshDebounceTimer.Stop();
                if (_tabControl.SelectedTab == _tabFiles) { _cacheManager.Clear(); await LadeInhalt(); }
            };
        }

        private void LoadSettingsFromDb()
        {
            var db = new DBHelper();
            string url = db.GetSetting(AppConfig.SettingsKeys.ApiUrl);
            string login = db.GetSetting(AppConfig.SettingsKeys.LoginUrl);
            if (!string.IsNullOrEmpty(url)) AppConfig.ApiBaseUrl = url;
            if (!string.IsNullOrEmpty(login)) AppConfig.LoginUrl = login;
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Activate();
        }

        private async Task LadeInhalt()
        {
            try
            {
                _thumbnailCts?.Cancel();
                _thumbnailCts = new CancellationTokenSource();
                UpdateBreadcrumbs();
                if (_seafileClient == null) _seafileClient = new SeafileClient(_authToken);

                lstRepos.BeginUpdate();
                lstRepos.Items.Clear();
                lstRepos.Groups.Clear();
                lstRepos.ShowGroups = true;

                string cacheKey = _navState.IsInRoot ? "root" : $"{_navState.CurrentRepoId}:{_navState.CurrentPath}";
                var cachedEntries = _cacheManager.Get<List<object>>(cacheKey);

                if (cachedEntries != null) RenderItems(cachedEntries);
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
                lstRepos.EndUpdate();
                if (!_navState.IsInRoot) _ = LoadThumbnailsAsync(_navState.CurrentRepoId, _navState.CurrentPath, _thumbnailCts.Token);
            }
            catch (Exception ex)
            {
                lstRepos.EndUpdate();
                UiHelper.ShowErrorDialog("Ladefehler", ex.Message);
            }
        }

        private void RenderItems(List<object> items)
        {
            if (_navState.IsInRoot) _ = LoadLibrariesUI(items.Cast<SeafileRepo>().ToList());
            else LoadDirectoryUI(items.Cast<SeafileEntry>().ToList());
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
                    ListViewGroup targetGroup = lstRepos.Groups.Cast<ListViewGroup>().FirstOrDefault(g => g.Header == groupName);
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

        private void LoadDirectoryUI(List<SeafileEntry> entries)
        {
            lstRepos.ShowGroups = false;
            var backItem = new ListViewItem(".. [Zurück]", "back") { Tag = new SeafileEntry { type = "back" } };
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
            List<ListViewItem> itemsToCheck = lstRepos.Items.Cast<ListViewItem>().ToList();
            foreach (var item in itemsToCheck)
            {
                if (token.IsCancellationRequested) return;
                if (!(item.Tag is SeafileEntry entry) || entry.type == "dir" || entry.type == "back") continue;

                string ext = System.IO.Path.GetExtension(entry.name).ToLower();
                if (new[] { ".jpg", ".png", ".jpeg", ".gif" }.Contains(ext))
                {
                    string fullPath = path.EndsWith("/") ?
                        path + entry.name : path + "/" + entry.name;
                    Image thumb = await _seafileClient.GetThumbnailAsync(repoId, fullPath, 48);

                    if (thumb != null && !token.IsCancellationRequested)
                    {
                        this.Invoke(new Action(() => {
                            string key = "thumb_" + entry.id;
                            if (!_repoIcons.Images.ContainsKey(key)) _repoIcons.Images.Add(key, thumb);
                            if (item.ListView != null) item.ImageKey = key;
                        }));
                    }
                }
            }
        }

        private void UpdateBreadcrumbs(string searchContext = null) => _breadcrumbManager?.Update(searchContext);

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
                        string newPath = _navState.CurrentPath.EndsWith("/") ?
                            _navState.CurrentPath + folderName : _navState.CurrentPath + "/" + folderName;
                        bool success = await _seafileClient.CreateDirectoryAsync(_navState.CurrentRepoId, newPath);
                        if (success)
                        {
                            _cacheManager.Clear();
                            await LadeInhalt();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Fehler", ex.Message);
            }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;

            string message = lstRepos.SelectedItems.Count == 1 ? GetDeleteMessage(lstRepos.SelectedItems[0].Tag) : $"Möchten Sie wirklich {lstRepos.SelectedItems.Count} Elemente unwiderruflich löschen?";
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
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Löschen teilweise fehlgeschlagen", ex.Message);
            }
        }

        private string GetDeleteMessage(object tag)
        {
            if (tag is SeafileEntry entry) return entry.type == "back" ?
                "" : $"Möchten Sie '{entry.name}' wirklich löschen?";
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
            if (tag is SeafileRepo repo) return await _seafileClient.DeleteLibraryAsync(repo.id);
            return false;
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = UiHelper.ShowInputDialog("Globale Suche", "Suchbegriff:");
            if (string.IsNullOrWhiteSpace(searchTerm)) { await LadeInhalt(); return; }

            lstRepos.Visible = false;
            lstRepos.Items.Clear(); lstRepos.Groups.Clear(); lstRepos.ShowGroups = true;
            try
            {
                lblStatus.Text = "Suche läuft...";
                UpdateBreadcrumbs("Suche: " + searchTerm);
                var allRepos = await _seafileClient.GetLibrariesAsync();
                var searchResults = new ConcurrentBag<ListViewItem>();
                var groups = new ConcurrentDictionary<string, ListViewGroup>();
                var searchTasks = new List<Task>();

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
                                    var item = new ListViewItem($"[{repo.name}] {path}", entry.type == "dir" ? "dir" : "file");
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
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Fehler", ex.Message);
            }
            finally
            {
                lstRepos.EndUpdate();
                lstRepos.Visible = true;
                lblStatus.Text = "Suche beendet."; UiHelper.UpdateColumnWidths(lstRepos);
            }
        }

        private async void CtxJumpTo_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            if (lstRepos.SelectedItems[0].Tag is Tuple<string, SeafileEntry> searchResult)
            {
                string repoId = searchResult.Item1;
                SeafileEntry entry = searchResult.Item2;
                string targetPath = entry.parent_dir ?? "/";
                targetPath = targetPath.Replace("\\", "/").Replace("//", "/");
                if (!targetPath.StartsWith("/")) targetPath = "/" + targetPath;

                _navState.EnterRepo(repoId, "...");
                if (targetPath != "/") _navState.SetPath(targetPath);
                if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].Enabled = true;
                lstRepos.Visible = true; await LadeInhalt();

                foreach (ListViewItem item in lstRepos.Items)
                {
                    if (item.Tag is SeafileEntry dirEntry && dirEntry.name == entry.name)
                    {
                        item.Selected = true;
                        item.Focused = true; item.EnsureVisible(); break;
                    }
                }
            }
            else UiHelper.ShowInfoDialog("Info", "Du bist bereits in diesem Verzeichnis oder Funktion hier nicht verfügbar.");
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
            else try
                {
                    HandleSingleDownload(lstRepos.SelectedItems[0].Tag);
                }
                catch (Exception ex)
                {
                    UiHelper.ShowErrorDialog("Fehler", ex.Message);
                }
        }

        private async void CtxPreview_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count != 1) return;
            if (!(lstRepos.SelectedItems[0].Tag is SeafileEntry entry) || entry.type == "dir" || entry.type == "back") return;

            await OpenPreview(entry);
        }

        private void CtxShare_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count != 1) return; // Nur Einzel-Freigabe supporten

            var tag = lstRepos.SelectedItems[0].Tag;
            string repoId = _navState.CurrentRepoId;
            string path = "";
            string name = lstRepos.SelectedItems[0].Text;
            bool isDir = false;

            if (tag is SeafileEntry entry)
            {
                if (entry.type == "back") return;

                path = _navState.CurrentPath.EndsWith("/")
                    ? _navState.CurrentPath + entry.name
                    : _navState.CurrentPath + "/" + entry.name;
                isDir = (entry.type == "dir");
            }
            else if (tag is SeafileRepo repo)
            {
                // Ganze Bibliothek freigeben (Technisch gesehen Root-Ordner "/")
                repoId = repo.id;
                path = "/";
                isDir = true;
                name = repo.name;
            }
            else
            {
                return;
            }

            // Form öffnen
            try
            {
                // path muss bereinigt werden (keine doppelten Slashes)
                path = path.Replace("//", "/");
                new FrmShare(_seafileClient, repoId, path, name, isDir).ShowDialog();
            }
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Fehler", ex.Message);
            }
        }

        private async Task OpenPreview(SeafileEntry entry)
        {
            try
            {
                lblStatus.Text = $"Öffne Vorschau für {entry.name}...";
                string ext = Path.GetExtension(entry.name).ToLower();

                bool useServerViewer = new HashSet<string> { ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".odt", ".ods", ".csv" }.Contains(ext);

                if (useServerViewer)
                {
                    string baseUrl = AppConfig.ApiBaseUrl.Replace("api2/", "").TrimEnd('/');
                    string fullPath = _navState.CurrentPath.EndsWith("/")
                        ? _navState.CurrentPath + entry.name
                        : _navState.CurrentPath + "/" + entry.name;

                    string encodedPath = string.Join("/", fullPath.Split('/').Select(Uri.EscapeDataString));
                    string webViewUrl = $"{baseUrl}/lib/{_navState.CurrentRepoId}/file{encodedPath}";

                    new FrmPreview(webViewUrl, entry.name, true).Show();
                    lblStatus.Text = "Online-Vorschau geöffnet.";
                }
                else
                {
                    string tempFile = Path.Combine(Path.GetTempPath(), entry.name);
                    if (!System.IO.File.Exists(tempFile) || new FileInfo(tempFile).Length != entry.size)
                    {
                        string fullPath = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + entry.name : _navState.CurrentPath + "/" + entry.name;
                        string link = await _seafileClient.GetDownloadLinkAsync(_navState.CurrentRepoId, fullPath);
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            var bytes = await client.GetByteArrayAsync(link);
                            await System.IO.File.WriteAllBytesAsync(tempFile, bytes);
                        }
                    }
                    new FrmPreview(tempFile, entry.name, false).Show();
                    lblStatus.Text = "Lokale Vorschau bereit.";
                }
            }
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Vorschau Fehler", ex.Message);
            }
            finally
            {
                await Task.Delay(3000);
                lblStatus.Text = $"{_navState.CurrentRepoName}: {_navState.CurrentPath}";
            }
        }

        private async void CtxRename_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count != 1) return;
            var tag = lstRepos.SelectedItems[0].Tag;
            string currentName = lstRepos.SelectedItems[0].Text;
            string type = "";

            if (tag is SeafileEntry entry)
            {
                if (entry.type == "back") return;
                type = entry.type;
            }
            else if (tag is SeafileRepo)
            {
                UiHelper.ShowInfoDialog("Info", "Bibliotheken können hier nicht umbenannt werden.");
                return;
            }

            string newName = UiHelper.ShowInputDialog("Umbenennen", $"Neuer Name für '{currentName}':");
            if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
            {
                try
                {
                    string path = _navState.CurrentPath.EndsWith("/") ? _navState.CurrentPath + currentName : _navState.CurrentPath + "/" + currentName;
                    bool isDir = (type == "dir");
                    bool success = await _seafileClient.RenameEntryAsync(_navState.CurrentRepoId, path, isDir, newName);

                    if (success)
                    {
                        _cacheManager.Clear();
                        await LadeInhalt();
                        new MaterialSnackBar("Erfolgreich umbenannt", "OK", true).Show(this);
                    }
                }
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
                string repoId = _navState.CurrentRepoId; string navPath = _navState.CurrentPath;
                if (tag is Tuple<string, SeafileEntry> tuple)
                {
                    repoId = tuple.Item1;
                    entryToDownload = tuple.Item2; navPath = entryToDownload.parent_dir ?? "/";
                }
                else if (tag is SeafileEntry entry) entryToDownload = entry;

                if (entryToDownload != null && entryToDownload.type != "back")
                {
                    _ = _downloadManager.DownloadEntryAsync(entryToDownload, repoId, navPath);
                    started = true;
                }
            }
            if (started) new MaterialSnackBar("Download gestartet!", "OK", true).Show(this);
        }

        private void CtxMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is ContextMenuStrip cms && lstRepos.SelectedItems.Count > 0)
            {
                var firstTag = lstRepos.SelectedItems[0].Tag;
                ToolStripItem itemJump = cms.Items["ItemJump"];
                if (itemJump != null) itemJump.Visible = firstTag is Tuple<string, SeafileEntry>;

                ToolStripItem itemPreview = cms.Items["ItemPreview"];
                if (itemPreview != null)
                {
                    itemPreview.Visible = false;
                    if (lstRepos.SelectedItems.Count == 1 && firstTag is SeafileEntry entry && entry.type != "dir" && entry.type != "back")
                    {
                        string ext = System.IO.Path.GetExtension(entry.name).ToLower();
                        var supportedExtensions = new HashSet<string>
                        {
                            ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg",
                            ".txt", ".md", ".cs", ".json", ".xml", ".html", ".js", ".css", ".log",
                            ".mp4", ".webm", ".mp3", ".wav",
                            ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".odt", ".ods", ".csv"
                        };
                        if (supportedExtensions.Contains(ext)) itemPreview.Visible = true;
                    }
                }
            }
            else if (lstRepos.SelectedItems.Count == 0) e.Cancel = true;
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
                if (entry.type == "back")
                {
                    _navState.GoBack();
                    await LadeInhalt();
                }
                else if (entry.type == "dir")
                {
                    _navState.EnterFolder(entry.name);
                    await LadeInhalt();
                }
                else CtxDownload_Click(sender, e);
            }
        }

        private int _sortColumn = -1;
        private bool _sortAscending = true;

        private void LstRepos_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != _sortColumn)
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }
            else
            {
                _sortAscending = !_sortAscending;
            }

            lstRepos.ListViewItemSorter = new ListViewSorter(e.Column, _sortAscending);
            lstRepos.Sort();
        }

        private void SetupTransferContextMenu()
        {
            ContextMenuStrip ctxTransfer = new ContextMenuStrip();
            ctxTransfer.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            ctxTransfer.BackColor = Color.FromArgb(40, 40, 40);
            ctxTransfer.ForeColor = Color.White;

            ToolStripMenuItem itemOpen = new ToolStripMenuItem("Im Ordner anzeigen");
            itemOpen.Click += (s, e) => {
                if (_lstDownloads.SelectedItems.Count > 0 && _lstDownloads.SelectedItems[0].Tag is DownloadItem item)
                {
                    if (!string.IsNullOrEmpty(item.LocalFilePath))
                    {
                        if (System.IO.File.Exists(item.LocalFilePath)) System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{item.LocalFilePath}\"");
                        else if (System.IO.Directory.Exists(item.LocalFilePath)) System.Diagnostics.Process.Start("explorer.exe", item.LocalFilePath);
                        else UiHelper.ShowErrorDialog("Fehler", "Datei nicht gefunden (evtl. gelöscht).");
                    }
                }
            };

            ToolStripMenuItem itemClear = new ToolStripMenuItem("Fertige entfernen");
            itemClear.Click += (s, e) => {
                _lstDownloads.BeginUpdate();
                for (int i = _lstDownloads.Items.Count - 1; i >= 0; i--)
                {
                    if (_lstDownloads.Items[i].Tag is DownloadItem item && item.Status == "Fertig") _lstDownloads.Items.RemoveAt(i);
                }
                _lstDownloads.EndUpdate();
            };

            ctxTransfer.Items.Add(itemOpen);
            ctxTransfer.Items.Add(new ToolStripSeparator());
            ctxTransfer.Items.Add(itemClear);
            _lstDownloads.ContextMenuStrip = ctxTransfer;
        }

        private void lstRepos_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is ListViewItem item) _dragCursor = CreateDragCursorWithText(item);
            lstRepos.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void LstRepos_DragOver(object sender, DragEventArgs e)
        {
            Point cp = lstRepos.PointToClient(new Point(e.X, e.Y));
            ListViewItem targetItem = lstRepos.GetItemAt(cp.X, cp.Y);
            int hoverIndex = (targetItem != null) ? targetItem.Index : -1;
            int currentIndex = (lstRepos.Tag is int val) ? val : -1;

            if (currentIndex != hoverIndex)
            {
                lstRepos.Tag = hoverIndex;
                lstRepos.Invalidate();
            }

            if (e.Data.GetDataPresent(typeof(ListViewItem))) e.Effect = DragDropEffects.Move;
            else if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void LstRepos_DragLeave(object sender, EventArgs e)
        {
            if ((lstRepos.Tag is int val) && val != -1)
            {
                lstRepos.Tag = -1;
                lstRepos.Invalidate();
            }
        }

        private void lstRepos_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;
            if ((e.Effect & DragDropEffects.Move) == DragDropEffects.Move && _dragCursor != null) Cursor.Current = _dragCursor;
            else Cursor.Current = Cursors.No;
        }

        private Cursor CreateDragCursorWithText(ListViewItem item)
        {
            Bitmap bmp = new Bitmap(300, 40);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(100, 50, 50, 50)))
                {
                    g.FillRectangle(bg, 0, 0, 300, 40);
                }
                if (item.ImageList != null && !string.IsNullOrEmpty(item.ImageKey))
                {
                    Image icon = item.ImageList.Images[item.ImageKey];
                    g.DrawImage(icon, 5, 5, 32, 32);
                }
                using (Font f = new Font("Segoe UI", 10, FontStyle.Bold))
                {
                    g.DrawString(item.Text, f, Brushes.White, 40, 10);
                }
                g.DrawRectangle(Pens.Orange, 0, 0, 299, 39);
            }
            return new Cursor(bmp.GetHicon());
        }

        private void LstRepos_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else if (e.Data.GetDataPresent(typeof(ListViewItem))) e.Effect = DragDropEffects.Move;
            else e.Effect = DragDropEffects.None;
        }

        private async void LstRepos_DragDrop(object sender, DragEventArgs e)
        {
            Cursor.Current = Cursors.Default;
            if (_dragCursor != null) { _dragCursor.Dispose(); _dragCursor = null; }
            lstRepos.Tag = -1;
            lstRepos.Invalidate();

            Point cp = lstRepos.PointToClient(new Point(e.X, e.Y));
            ListViewItem targetItem = lstRepos.GetItemAt(cp.X, cp.Y);

            string uploadTargetDir = _navState.CurrentPath;

            if (targetItem != null && targetItem.Tag is SeafileEntry entry && entry.type == "dir")
            {
                uploadTargetDir = uploadTargetDir.EndsWith("/")
                    ? uploadTargetDir + entry.name
                    : uploadTargetDir + "/" + entry.name;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (_navState.IsInRoot && targetItem == null)
                {
                    UiHelper.ShowInfoDialog("Info", "Bitte öffne erst eine Bibliothek oder ziehe die Dateien auf einen Ordner.");
                    return;
                }

                if (_navState.IsInRoot && targetItem != null && targetItem.Tag is SeafileRepo repo)
                {
                    try
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        _ = _downloadManager.UploadFilesAsync(files, repo.id, "/");
                        new MaterialSnackBar($"Upload in Bibliothek '{repo.name}' gestartet!", "OK", true).Show(this);
                    }
                    catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
                    return;
                }

                string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (droppedPaths != null && droppedPaths.Length > 0)
                {
                    try
                    {
                        _ = _downloadManager.UploadFilesAsync(droppedPaths, _navState.CurrentRepoId, uploadTargetDir);
                        string msg = uploadTargetDir == _navState.CurrentPath
                            ? "Upload gestartet!"
                            : $"Upload in '{Path.GetFileName(uploadTargetDir)}' gestartet!";
                        new MaterialSnackBar(msg, "OK", true).Show(this);
                    }
                    catch (Exception ex) { UiHelper.ShowScrollableErrorDialog("Upload Fehler", ex.Message); }
                }
                return;
            }

            if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                var srcItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));
                if (srcItem.Tag is SeafileRepo) { UiHelper.ShowInfoDialog("Nicht möglich", "Bibliotheken können nicht verschoben werden."); return; }
                if (!(srcItem.Tag is SeafileEntry srcEntry)) return;

                if (targetItem == null) return;
                if (!(targetItem.Tag is SeafileEntry targetEntry)) return;

                if (targetEntry.type != "dir" && targetEntry.type != "back") return;
                if (srcEntry.id == targetEntry.id) return;

                string currentDir = _navState.CurrentPath;
                if (!currentDir.EndsWith("/")) currentDir += "/";
                string dstDir = "";
                string targetNameDisplay = "";

                if (targetEntry.type == "back")
                {
                    if (_navState.CurrentPath == "/" || string.IsNullOrEmpty(_navState.CurrentPath)) { UiHelper.ShowInfoDialog("Nicht möglich", "Du bist bereits im Hauptverzeichnis."); return; }
                    string cleanPath = _navState.CurrentPath.TrimEnd('/');
                    int lastSlashIndex = cleanPath.LastIndexOf('/');
                    if (lastSlashIndex <= 0) dstDir = "/"; else dstDir = cleanPath.Substring(0, lastSlashIndex + 1);
                    targetNameDisplay = "den übergeordneten Ordner";
                }
                else
                {
                    dstDir = currentDir + targetEntry.name;
                    targetNameDisplay = $"'{targetEntry.name}'";
                }

                string typeLabel = srcEntry.type == "dir" ? "den Ordner" : "die Datei";
                if (!UiHelper.ShowConfirmationDialog("Verschieben", $"Möchtest du {typeLabel} '{srcEntry.name}' in {targetNameDisplay} verschieben?")) return;

                try
                {
                    lblStatus.Text = "Verschiebe...";
                    string srcPath = currentDir + srcEntry.name;
                    bool isDir = srcEntry.type == "dir";
                    try
                    {
                        bool success = await _seafileClient.MoveEntryAsync(_navState.CurrentRepoId, srcPath, dstDir, isDir);
                        if (success) { new MaterialSnackBar("Erfolgreich verschoben", "OK", true).Show(this); _cacheManager.Clear(); await LadeInhalt(); return; }
                    }
                    catch (Exception moveEx)
                    {
                        string warningMsg = $"Direktes Verschieben fehlgeschlagen: {moveEx.Message}\n\nKopieren + Löschen versuchen?";
                        if (UiHelper.ShowDangerConfirmation("Verschieben fehlgeschlagen", warningMsg))
                        {
                            lblStatus.Text = "Kopiere...";
                            bool copySuccess = await _seafileClient.CopyEntryAsync(_navState.CurrentRepoId, srcPath, dstDir, isDir);
                            if (copySuccess)
                            {
                                lblStatus.Text = "Lösche Original...";
                                bool deleteSuccess = await _seafileClient.DeleteEntryAsync(_navState.CurrentRepoId, srcPath, isDir);
                                if (deleteSuccess) { new MaterialSnackBar("Verschoben (Copy+Delete).", "OK", true).Show(this); _cacheManager.Clear(); await LadeInhalt(); }
                                else { UiHelper.ShowErrorDialog("Teilerfolg", "Kopiert, aber Original konnte nicht gelöscht werden."); await LadeInhalt(); }
                            }
                        }
                    }
                }
                catch (Exception ex) { UiHelper.ShowScrollableErrorDialog("Fehler beim Verschieben", ex.Message); }
                finally { lblStatus.Text = $"{_navState.CurrentRepoName}: {_navState.CurrentPath}"; }
            }
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            _cacheManager.Clear();
            await LadeInhalt();
            new MaterialSnackBar("Aktualisiert.", 1000).Show(this);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (UiHelper.ShowConfirmationDialog("Abmelden", "Möchtest du dich wirklich abmelden?"))
            {
                new DBHelper().DeleteToken();
                new AuthManager(null).ClearBrowserCacheOnDisk(); Application.Restart();
            }
        }

        private void AddDownloadToUi(DownloadItem item)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<DownloadItem>(AddDownloadToUi), item); return; }

            string cleanName = item.FileName.Replace("⬇", "").Replace("⬆", "").Trim();
            var lvi = new ListViewItem(cleanName);
            lvi.SubItems.Add(item.Status);
            lvi.SubItems.Add("-");
            lvi.SubItems.Add(item.Progress + "%");
            lvi.SubItems.Add(item.StartTime.ToShortTimeString());

            // NEU: Dynamische Icons
            if (item.Type == "Ordner Upload")
            {
                lvi.ImageKey = "dir";
            }
            else if (item.Type == "Upload")
            {
                string ext = Path.GetExtension(cleanName).ToLower();
                var imageList = _lstDownloads.SmallImageList;

                if (imageList != null)
                {
                    if (!imageList.Images.ContainsKey(ext))
                    {
                        Icon sysIcon = IconHelper.GetIconForExtension(ext, false);
                        if (sysIcon != null) imageList.Images.Add(ext, sysIcon);
                        else imageList.Images.Add(ext, Properties.Resources.icon_file);
                    }
                    lvi.ImageKey = ext;
                }
            }
            else
            {
                lvi.ImageKey = "download"; // Fallback für Downloads
            }

            lvi.Tag = item; item.Tag = lvi;
            _lstDownloads.Items.Add(lvi);
            ResizeDownloadListColumns();
        }

        private void UpdateDownloadInUi(DownloadItem item)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<DownloadItem>(UpdateDownloadInUi), item); return; }
            if (item.Tag is ListViewItem lvi)
            {
                lvi.SubItems[1].Text = item.Status;
                lvi.SubItems[2].Text = item.SpeedString;
                lvi.SubItems[3].Text = item.Progress + "%";

                if (item.Status == "Fertig")
                {
                    lvi.ForeColor = Color.LightGreen;

                    // ICON-LOGIK FIX:
                    // Nur bei Downloads oder Ordnern den Haken setzen.
                    // Bei Einzeldateien wollen wir das Icon (PDF, Word etc.) behalten!
                    if (item.Type == "Batch Download" || item.Type.Contains("Bibliothek") || item.Type == "Ordner Upload")
                    {
                        lvi.ImageKey = "ok";
                    }
                    // Ansonsten: Icon beibehalten (es ist ja schon das richtige)
                }
                else if (item.Status.StartsWith("Fehler") || item.Status == "Abgebrochen")
                {
                    lvi.ForeColor = Color.Salmon;
                    lvi.ImageKey = "error";
                }
            }
            if (item.Status == "Fertig" && item.Type.Contains("Upload") && _refreshDebounceTimer != null) { _refreshDebounceTimer.Stop(); _refreshDebounceTimer.Start(); }
        }

        private void _lstDownloads_DoubleClick(object sender, EventArgs e)
        {
            if (_lstDownloads.SelectedItems.Count > 0 && _lstDownloads.SelectedItems[0].Tag is DownloadItem item) new FrmTransferDetail(item).ShowDialog();
        }

        private async void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string term = _txtSearch.Text.Trim();
                if (string.IsNullOrWhiteSpace(term))
                {
                    await LadeInhalt();
                    return;
                }
                PerformSearch(term);
            }
        }

        private async void PerformSearch(string searchTerm)
        {
            lstRepos.Visible = false;
            lstRepos.Items.Clear(); lstRepos.Groups.Clear(); lstRepos.ShowGroups = true;
            try
            {
                lblStatus.Text = "Suche läuft...";
                UpdateBreadcrumbs("Suche: " + searchTerm);
                var allRepos = await _seafileClient.GetLibrariesAsync();
                var searchResults = new ConcurrentBag<ListViewItem>();
                var groups = new ConcurrentDictionary<string, ListViewGroup>();
                var searchTasks = new List<Task>();

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
                                    var item = new ListViewItem($"[{repo.name}] {path}", entry.type == "dir" ? "dir" : "file");
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
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Fehler", ex.Message);
            }
            finally
            {
                lstRepos.EndUpdate();
                lstRepos.Visible = true;
                lblStatus.Text = "Suche beendet."; UiHelper.UpdateColumnWidths(lstRepos);
            }
        }

        private string FormatDate(long timestamp) => timestamp == 0 ? "-" : DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime().ToString("g");
    }
}