using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Helper;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp3.Controls;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class Form1
    {
        private MaterialTabControl _tabControl;
        private MaterialTabSelector _tabSelector;
        private System.Windows.Forms.TabPage _tabFiles;
        private System.Windows.Forms.TabPage _tabDownloads;
        private MaterialListView _lstDownloads;
        private FlowLayoutPanel _flowPath;
        private ImageList _repoIcons;
        private PictureBox _appIcon;

        // V1.4 Variables
        private TextBox _txtSearch;
        private ToolTip _actionToolTip;

        private void InitializeUiComponents()
        {
            SetupMaterialSkin();
            InitializeIcons();
            InitializeLogoResource();
            InitializeTabs();
            InitializeCustomUI();
            InitializeBreadcrumbsWrapper();
        }

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
            _lstDownloads.Columns.Add("Rate", 120);
            _lstDownloads.Columns.Add("Fortschritt", 100);
            _lstDownloads.Columns.Add("Startzeit", 120);

            UiHelper.SetupListView(_lstDownloads);
            ImageList downloadIcons = new ImageList { ImageSize = new Size(24, 24), ColorDepth = ColorDepth.Depth32Bit };
            downloadIcons.Images.Add("upload", Properties.Resources.icon_upload);
            downloadIcons.Images.Add("download", Properties.Resources.icon_download);
            downloadIcons.Images.Add("ok", Properties.Resources.Status_ok);
            downloadIcons.Images.Add("error", Properties.Resources.Status_error);
            _lstDownloads.SmallImageList = downloadIcons;

            _lstDownloads.SizeChanged += (s, e) => UiHelper.UpdateTransferColumnWidths(_lstDownloads);
            _lstDownloads.ColumnWidthChanging += (s, e) => {
                e.Cancel = true;
                e.NewWidth = _lstDownloads.Columns[e.ColumnIndex].Width;
            };
            _lstDownloads.DoubleClick += _lstDownloads_DoubleClick;
        }

        private void InitializeBreadcrumbsWrapper()
        {
            Control actionPanel = null;
            if (_tabFiles.Controls.ContainsKey("panelActionbar")) actionPanel = _tabFiles.Controls["panelActionbar"];

            if (actionPanel == null) return;

            int startX = 280;
            if (actionPanel.Controls.ContainsKey("btnDelete"))
            {
                var btnDel = actionPanel.Controls["btnDelete"];
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
            _flowPath.BringToFront();

            _breadcrumbManager = new BreadcrumbManager(
                _flowPath,
                _navState,
                async () => { _navState.ResetToRoot(); await LadeInhalt(); },
                async (id, name) => { _navState.EnterRepo(id, name); await LadeInhalt(); },
                null
            );
        }

        private void InitializeCustomUI()
        {
            // Context Menu mit Rename
            ContextMenuStrip ctxMenu = MenuBuilder.CreateContextMenu(CtxDownload_Click, BtnDelete_Click, CtxRename_Click);
            ToolStripMenuItem itemJump = new ToolStripMenuItem("Gehe zu") { Name = "ItemJump", Image = MenuBuilder.ResizeIcon(Properties.Resources.icon_ctx_jump, 16, 16) };
            itemJump.Click += CtxJumpTo_Click;
            ctxMenu.Items.Insert(0, new ToolStripSeparator());
            ctxMenu.Items.Insert(0, itemJump);

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

            // SORTIEREN (V1.4)
            lstRepos.ColumnClick += LstRepos_ColumnClick;

            // DRAG & DROP
            lstRepos.AllowDrop = true;
            lstRepos.ItemDrag += lstRepos_ItemDrag;
            lstRepos.DragEnter += LstRepos_DragEnter;
            lstRepos.DragOver += LstRepos_DragOver;
            lstRepos.DragDrop += LstRepos_DragDrop;
            lstRepos.GiveFeedback += lstRepos_GiveFeedback;
            lstRepos.DragLeave += LstRepos_DragLeave;

            ReplaceMaterialButtonsWithStandard();
            try
            {
                if (Controls.ContainsKey("panelActionbar")) Controls["panelActionbar"].BackColor = Color.FromArgb(45, 45, 48);
            }
            catch { }

            // TRANSFER MENU
            SetupTransferContextMenu();
        }

        private void ReplaceMaterialButtonsWithStandard()
        {
            Control actionPanel = null;
            if (Controls.ContainsKey("panelActionbar")) actionPanel = Controls["panelActionbar"];
            else if (_tabFiles.Controls.ContainsKey("panelActionbar")) actionPanel = _tabFiles.Controls["panelActionbar"];

            if (actionPanel == null) return;

            var toRemove = actionPanel.Controls.OfType<Control>()
                .Where(c => c is MaterialButton || c.Name.StartsWith("materialButton") || c.Name == "btnLogout" || c.Name == "btnSettings" || c.Name == "btnSearch" || c is System.Windows.Forms.Button || c is TextBox || c.Name == "pnlSearchContainer")
                .ToList();
            foreach (var c in toRemove) actionPanel.Controls.Remove(c);

            actionPanel.BackColor = Color.FromArgb(45, 45, 48);

            if (_appIcon != null)
            {
                _appIcon.Parent = actionPanel;
                _appIcon.Location = new Point(10, (actionPanel.Height - _appIcon.Height) / 2);
                if (!actionPanel.Controls.Contains(_appIcon)) actionPanel.Controls.Add(_appIcon);
                _appIcon.BringToFront();
            }

            if (_actionToolTip == null)
            {
                _actionToolTip = new ToolTip();
                _actionToolTip.AutoPopDelay = 5000;
                _actionToolTip.InitialDelay = 500;
                _actionToolTip.ReshowDelay = 200;
                _actionToolTip.ShowAlways = true;
            }
            _actionToolTip.RemoveAll();

            int leftX = (_appIcon != null) ? _appIcon.Right + 15 : 10;
            int btnY = 12;
            int rightEdge = actionPanel.Width - 10;

            // RECHTS
            System.Windows.Forms.Button btnSettings = CreateFlatButton("", Properties.Resources.icon_settings);
            btnSettings.Text = "";
            btnSettings.Width = 40;
            btnSettings.Name = "btnSettings";
            btnSettings.Location = new Point(rightEdge - btnSettings.Width, btnY);
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Click += (s, e) => new FrmSettings().ShowDialog();
            _actionToolTip.SetToolTip(btnSettings, "Einstellungen");
            actionPanel.Controls.Add(btnSettings);

            System.Windows.Forms.Button btnOut = CreateFlatButton("", Properties.Resources.icon_logout);
            btnOut.Text = "";
            btnOut.Width = 40;
            btnOut.Name = "btnLogout";
            btnOut.Location = new Point(btnSettings.Left - 5 - btnOut.Width, btnY);
            btnOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOut.Click += btnLogout_Click;
            _actionToolTip.SetToolTip(btnOut, "Abmelden");
            actionPanel.Controls.Add(btnOut);

            // SEARCH CONTAINER (Hier war der Panel Fehler -> System.Windows.Forms.Panel fixt es)
            System.Windows.Forms.Panel pnlSearch = new System.Windows.Forms.Panel();
            pnlSearch.Name = "pnlSearchContainer";
            pnlSearch.BackColor = Color.FromArgb(60, 60, 65);
            pnlSearch.Size = new Size(240, 38);
            pnlSearch.Location = new Point(btnOut.Left - 15 - pnlSearch.Width, btnY);
            pnlSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            pnlSearch.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(80, 80, 80)))
                    e.Graphics.DrawRectangle(p, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            PictureBox picSearch = new PictureBox
            {
                Image = ResizeImage(Properties.Resources.icon_search, 20, 20),
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent,
                Location = new Point(5, 7),
                Cursor = Cursors.IBeam
            };
            picSearch.Click += (s, e) => _txtSearch?.Focus();

            _txtSearch = new TextBox();
            _txtSearch.BorderStyle = BorderStyle.None;
            _txtSearch.BackColor = pnlSearch.BackColor;
            _txtSearch.ForeColor = Color.WhiteSmoke;
            _txtSearch.Font = new Font("Segoe UI", 11F);
            _txtSearch.Location = new Point(35, 9);
            _txtSearch.Width = pnlSearch.Width - 45;
            _txtSearch.PlaceholderText = "Suchen...";
            _txtSearch.KeyDown += TxtSearch_KeyDown;

            pnlSearch.Controls.Add(picSearch);
            pnlSearch.Controls.Add(_txtSearch);
            _actionToolTip.SetToolTip(pnlSearch, "Tippen & Enter drücken");
            _actionToolTip.SetToolTip(picSearch, "Suche starten");
            actionPanel.Controls.Add(pnlSearch);

            // LINKS
            System.Windows.Forms.Button btnNew = CreateFlatButton("NEU", Properties.Resources.icon_new);
            btnNew.Name = "btnNew";
            btnNew.Location = new Point(leftX, btnY);
            btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnNew.Click += BtnNew_Click;
            _actionToolTip.SetToolTip(btnNew, "Neue Bibliothek oder Ordner erstellen");
            actionPanel.Controls.Add(btnNew);

            System.Windows.Forms.Button btnDel = CreateFlatButton("LÖSCHEN", Properties.Resources.icon_delete);
            btnDel.Name = "btnDelete";
            btnDel.Location = new Point(btnNew.Right + 10, btnY);
            btnDel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnDel.Click += BtnDelete_Click;
            _actionToolTip.SetToolTip(btnDel, "Markierte Elemente löschen");
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
            btn.Width = 24 + 10 + textSize.Width + 10;
            return btn;
        }

        private Image ResizeImage(Image img, int w, int h) => new Bitmap(img, new Size(w, h));
        private void ResizeDownloadListColumns()
        {
            if (_lstDownloads.Columns.Count < 5 || _lstDownloads.ClientSize.Width == 0) return;
            UiHelper.UpdateTransferColumnWidths(_lstDownloads);
        }
    }
}