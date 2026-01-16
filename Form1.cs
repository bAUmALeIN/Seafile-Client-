using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Util;
using System.Drawing;

namespace WinFormsApp3
{
    public partial class Form1 : MaterialForm
    {
        private SeafileClient _seafileClient;
        private NavigationState _navState;
        private readonly string _authToken;

        public Form1(string token)
        {
            InitializeComponent();
            _authToken = token;
            _navState = new NavigationState(); 


            SetupMaterialSkin();
            UiHelper.SetupListView(lstRepos);


            lstRepos.DoubleClick += lstRepos_DoubleClick;

            lstRepos.SizeChanged += (s, e) => UiHelper.UpdateColumnWidths(lstRepos);

            lstRepos.ColumnWidthChanging += (s, e) => { e.Cancel = true; e.NewWidth = lstRepos.Columns[e.ColumnIndex].Width; };


            materialButton2.Click += BtnNew_Click;
            materialButton3.Click += BtnDelete_Click;

            // Panels Farben fixen
            try { if (Controls.ContainsKey("panelSidebar")) Controls["panelSidebar"].BackColor = Color.FromArgb(45, 45, 48); } catch { }
            try { if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].BackColor = Color.FromArgb(45, 45, 48); } catch { }
            try { if (Controls.ContainsKey("parrotPictureBoxLogo")) parrotPictureBoxLogo.BackColor = Color.FromArgb(45, 45, 48); } catch { }

            lstRepos.Visible = false;
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

        
        private async Task LadeInhalt()
        {
            try
            {
                if (_seafileClient == null) _seafileClient = new SeafileClient(_authToken);

                lstRepos.Items.Clear();

                if (_navState.IsInRoot)
                {
                    // Bibliotheken laden
                    var repos = await _seafileClient.GetLibrariesAsync();
                    foreach (var repo in repos)
                    {
                        var item = new ListViewItem("📚");
                        item.SubItems.Add(repo.name);
                        item.SubItems.Add((repo.size / 1024 / 1024) + " MB");
                        item.SubItems.Add("Repo");
                        item.Tag = repo;
                        lstRepos.Items.Add(item);
                    }
                    lblStatus.Text = $"Bereit. {repos.Count} Bibliotheken.";
                }
                else
                {
                    // Ordner Inhalt laden
                    var entries = await _seafileClient.GetDirectoryEntriesAsync(_navState.CurrentRepoId, _navState.CurrentPath);

                    
                    var backItem = new ListViewItem("🔙");
                    backItem.SubItems.Add(".. [Zurück]");
                    backItem.SubItems.Add(""); // Größe leer
                    backItem.SubItems.Add(""); // Typ leer
                    backItem.Tag = new SeafileEntry { type = "back" };
                    lstRepos.Items.Add(backItem);

                    foreach (var entry in entries)
                    {
                        string icon = entry.type == "dir" ? "📁" : "📄";
                        var item = new ListViewItem(icon);
                        item.SubItems.Add(entry.name);
                        string sizeText = entry.type == "dir" ? "-" : (entry.size / 1024) + " KB";
                        item.SubItems.Add(sizeText);
                        item.SubItems.Add(entry.type);
                        item.Tag = entry;
                        lstRepos.Items.Add(item);
                    }
                    lblStatus.Text = $"Ordner: {_navState.CurrentPath}";
                }

                lstRepos.Visible = true;
                UiHelper.UpdateColumnWidths(lstRepos);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
        }

        // --- Event Handler ---

        private async void lstRepos_DoubleClick(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            var tag = lstRepos.SelectedItems[0].Tag;

            if (tag is SeafileRepo repo)
            {
                _navState.EnterRepo(repo.id);
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
            }
        }


        private async void BtnNew_Click(object sender, EventArgs e)
        {
            if (_navState.IsInRoot)
            {
                MessageBox.Show("Bitte wähle zuerst eine Bibliothek aus.");
                return;
            }

            string folderName = UiHelper.ShowInputDialog("Neuer Ordner", "Name des Ordners:");
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                string newPath = _navState.CurrentPath.EndsWith("/")
                    ? _navState.CurrentPath + folderName
                    : _navState.CurrentPath + "/" + folderName;

                bool success = await _seafileClient.CreateDirectoryAsync(_navState.CurrentRepoId, newPath);

                if (success) await LadeInhalt();
                else MessageBox.Show("Fehler beim Erstellen.");
            }
        }


        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItems.Count == 0) return;
            var tag = lstRepos.SelectedItems[0].Tag;

            if (tag is SeafileEntry entry && entry.type != "back")
            {
                if (MessageBox.Show($"'{entry.name}' wirklich löschen?", "Löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    string pathToDelete = _navState.CurrentPath.EndsWith("/")
                        ? _navState.CurrentPath + entry.name
                        : _navState.CurrentPath + "/" + entry.name;

                    bool isDir = entry.type == "dir";
                    bool success = await _seafileClient.DeleteEntryAsync(_navState.CurrentRepoId, pathToDelete, isDir);

                    if (success) await LadeInhalt();
                    else MessageBox.Show("Fehler beim Löschen.");
                }
            }
            else
            {
                MessageBox.Show("Bitte eine Datei oder Ordner auswählen (nicht Bibliotheken).");
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Abmelden?", "Logout", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    new DBHelper().DeleteToken();
                    new AuthManager(null).ClearBrowserCacheOnDisk();
                    Application.Restart();
                    Environment.Exit(0);
                }
                catch { }
            }
        }

        // --- Override ---

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UiHelper.UpdateColumnWidths(lstRepos);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Activate();
        }
    }
}