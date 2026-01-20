using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class FrmShare : MaterialForm
    {
        private readonly SeafileClient _client;
        private readonly string _repoId;
        private readonly string _path;
        private readonly string _itemName;
        private readonly bool _isDir;

        // UI Controls
        private MaterialTabControl _tabControl;
        private MaterialTabSelector _tabSelector;

        // Tab 1
        private MaterialCheckBox _chkPassword;
        private MaterialTextBoxEdit _txtPassword;
        private MaterialCheckBox _chkExpire;
        private NumericUpDown _numExpireDays;
        private MaterialRadioButton _rbDownload;
        private MaterialRadioButton _rbPreview;
        private MaterialButton _btnCreate;

        // Tab 2
        private MaterialListView _lstLinks;
        private MaterialButton _btnCopyLink;
        private Label _lblListStatus;

        // Tab 3
        private MaterialTextBoxEdit _txtEmail;
        private MaterialComboBox _cmbPermission;
        private MaterialButton _btnShareUser;

        public FrmShare(SeafileClient client, string repoId, string path, string itemName, bool isDir)
        {
            _client = client;
            _repoId = repoId;
            _path = path;
            _itemName = itemName;
            _isDir = isDir;

            MaterialSkinManager.Instance.AddFormToManage(this);
            InitializeComponentUI();

            this.Load += async (s, e) => await RefreshLinkList();
        }

        private void InitializeComponentUI()
        {
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = $"Freigabe: {_itemName}";
            this.Sizable = false;

            _tabSelector = new MaterialTabSelector { Depth = 0, Location = new Point(0, 64), Size = new Size(this.Width, 48), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _tabControl = new MaterialTabControl { Location = new Point(0, 112), Size = new Size(this.Width, this.Height - 112), Depth = 0 };
            _tabSelector.BaseTabControl = _tabControl;

            System.Windows.Forms.TabPage tabCreate = new System.Windows.Forms.TabPage("Neuer Link") { BackColor = Color.FromArgb(50, 50, 50) };
            SetupTabCreate(tabCreate);
            _tabControl.Controls.Add(tabCreate);

            System.Windows.Forms.TabPage tabManage = new System.Windows.Forms.TabPage("Links verwalten") { BackColor = Color.FromArgb(50, 50, 50) };
            SetupTabManage(tabManage);
            _tabControl.Controls.Add(tabManage);

            System.Windows.Forms.TabPage tabUser = new System.Windows.Forms.TabPage("An Person senden") { BackColor = Color.FromArgb(50, 50, 50) };
            SetupTabUser(tabUser);
            _tabControl.Controls.Add(tabUser);

            this.Controls.Add(_tabSelector);
            this.Controls.Add(_tabControl);
        }

        private void SetupTabCreate(System.Windows.Forms.TabPage tab)
        {
            int y = 20;
            Label lblPerm = CreateHeaderLabel("Berechtigung festlegen", 20, y);
            tab.Controls.Add(lblPerm);
            y += 30;

            _rbDownload = new MaterialRadioButton { Text = "Vorschau und Herunterladen", Checked = true, Location = new Point(20, y), Size = new Size(300, 37) };
            _rbPreview = new MaterialRadioButton { Text = "Nur Vorschau erlaubt", Location = new Point(20, y + 40), Size = new Size(300, 37) };
            tab.Controls.Add(_rbDownload);
            tab.Controls.Add(_rbPreview);
            y += 90;

            _chkPassword = new MaterialCheckBox { Text = "Passwortschutz", Location = new Point(20, y), Size = new Size(200, 37) };
            _chkPassword.CheckedChanged += (s, e) => _txtPassword.Enabled = _chkPassword.Checked;
            tab.Controls.Add(_chkPassword);

            _txtPassword = new MaterialTextBoxEdit { Hint = "Passwort eingeben", Location = new Point(230, y - 5), Size = new Size(200, 50), Enabled = false, UseAccent = true };
            tab.Controls.Add(_txtPassword);
            y += 60;

            _chkExpire = new MaterialCheckBox { Text = "Ablaufdatum (Tage)", Location = new Point(20, y), Size = new Size(200, 37) };
            _chkExpire.CheckedChanged += (s, e) => _numExpireDays.Enabled = _chkExpire.Checked;
            tab.Controls.Add(_chkExpire);

            _numExpireDays = new NumericUpDown { Location = new Point(230, y + 8), Size = new Size(80, 30), Minimum = 1, Maximum = 365, Value = 7, Enabled = false, Font = new Font("Segoe UI", 12f) };
            tab.Controls.Add(_numExpireDays);
            y += 80;

            _btnCreate = new MaterialButton { Text = "Link erstellen", Location = new Point(20, y), Size = new Size(410, 40), Type = MaterialButton.MaterialButtonType.Contained, UseAccentColor = false };
            _btnCreate.Click += async (s, e) => await CreateLinkAction();
            tab.Controls.Add(_btnCreate);
        }

        private void SetupTabManage(System.Windows.Forms.TabPage tab)
        {
            _lblListStatus = CreateHeaderLabel("Lade Links...", 20, 15);
            _lblListStatus.ForeColor = Color.Gray;
            tab.Controls.Add(_lblListStatus);

            _lstLinks = new MaterialListView
            {
                Location = new Point(20, 50),
                Size = new Size(640, 280),
                FullRowSelect = true,
                View = View.Details,
                OwnerDraw = true,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };

            _lstLinks.Columns.Add("Link (Token)", 300);
            _lstLinks.Columns.Add("Typ", 100);
            _lstLinks.Columns.Add("Views", 80);
            _lstLinks.Columns.Add("Ablauf", 120);

            UiHelper.SetupListView(_lstLinks);
            tab.Controls.Add(_lstLinks);

            _btnCopyLink = new MaterialButton { Text = "Kopieren", Location = new Point(530, 340), Size = new Size(130, 36), Type = MaterialButton.MaterialButtonType.Contained };
            _btnCopyLink.Click += (s, e) => CopySelectedLink();
            tab.Controls.Add(_btnCopyLink);

            var btnRefresh = new MaterialButton { Text = "Aktualisieren", Location = new Point(20, 340), Size = new Size(150, 36), Type = MaterialButton.MaterialButtonType.Outlined };
            btnRefresh.Click += async (s, e) => await RefreshLinkList();
            tab.Controls.Add(btnRefresh);
        }

        private void SetupTabUser(System.Windows.Forms.TabPage tab)
        {
            tab.Controls.Add(CreateHeaderLabel("An Seafile-Nutzer freigeben", 20, 20));

            _txtEmail = new MaterialTextBoxEdit { Hint = "E-Mail Adresse", Location = new Point(20, 60), Size = new Size(400, 50) };
            tab.Controls.Add(_txtEmail);

            _cmbPermission = new MaterialComboBox { Hint = "Rechte", Location = new Point(20, 130), Size = new Size(200, 49) };
            _cmbPermission.Items.Add("Lesen & Schreiben (rw)");
            _cmbPermission.Items.Add("Nur Lesen (r)");
            _cmbPermission.SelectedIndex = 0;
            tab.Controls.Add(_cmbPermission);

            _btnShareUser = new MaterialButton { Text = "Freigeben", Location = new Point(20, 200), Size = new Size(200, 36), Type = MaterialButton.MaterialButtonType.Contained, UseAccentColor = false };
            _btnShareUser.Click += async (s, e) => await ShareToUserAction();
            tab.Controls.Add(_btnShareUser);
        }

        private async Task RefreshLinkList()
        {
            try
            {
                _lblListStatus.Text = "Frage Server ab...";
                _lstLinks.Items.Clear();

                List<SeafileShareLink> links = await _client.GetShareLinksAsync(_repoId, _path);

                _lstLinks.BeginUpdate();
                foreach (var link in links)
                {
                    var item = new ListViewItem(link.link);
                    item.SubItems.Add(link.CanDownload ? "Download" : "Vorschau");
                    item.SubItems.Add(link.view_cnt.ToString());

                    string expire = string.IsNullOrEmpty(link.expire_date) ? "Nie" : link.expire_date;
                    if (link.is_expired) expire += " (Abgelaufen)";
                    item.SubItems.Add(expire);

                    item.Tag = link;
                    _lstLinks.Items.Add(item);
                }
                _lstLinks.EndUpdate();

                if (links.Count > 0)
                {
                    _lblListStatus.Text = $"{links.Count} Link(s) gefunden.";
                    _lblListStatus.ForeColor = Color.LightGreen;
                    _tabControl.SelectedIndex = 1;
                }
                else
                {
                    _lblListStatus.Text = "Keine öffentlichen Links vorhanden.";
                    _lblListStatus.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                _lblListStatus.Text = "Fehler beim Laden.";
                UiHelper.ShowErrorDialog("Fehler", ex.Message);
            }
        }

        private async Task CreateLinkAction()
        {
            string pass = _chkPassword.Checked ? _txtPassword.Text : null;
            int days = _chkExpire.Checked ? (int)_numExpireDays.Value : 0;
            bool canDownload = _rbDownload.Checked;

            try
            {
                _btnCreate.Enabled = false;
                _btnCreate.Text = "Sende...";

                string link = await _client.CreateShareLinkAsync(_repoId, _path, pass, days, canDownload);

                UiHelper.ShowSuccessDialog("Erfolg", "Link wurde erstellt.");
                Clipboard.SetText(link);
                await RefreshLinkList();
                _tabControl.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                if (ex.Message == "LINK_EXISTS")
                {
                    UiHelper.ShowInfoDialog("Info", "Ein identischer Link existiert bereits.");
                    await RefreshLinkList();
                }
                else
                {
                    UiHelper.ShowErrorDialog("Fehler", ex.Message);
                }
            }
            finally
            {
                _btnCreate.Enabled = true;
                _btnCreate.Text = "Link erstellen";
            }
        }

        private void CopySelectedLink()
        {
            if (_lstLinks.SelectedItems.Count > 0 && _lstLinks.SelectedItems[0].Tag is SeafileShareLink link)
            {
                Clipboard.SetText(link.link);
                new MaterialSnackBar("Link kopiert", 1000).Show(this);
            }
        }

        private async Task ShareToUserAction()
        {
            string email = _txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email)) return;
            string perm = _cmbPermission.SelectedIndex == 0 ? "rw" : "r";

            try
            {
                _btnShareUser.Enabled = false;
                await _client.ShareToUserAsync(_repoId, _path, email, _isDir, perm);
                UiHelper.ShowSuccessDialog("Gesendet", $"Freigabe für '{email}' erfolgt.");
                _txtEmail.Text = "";
            }
            catch (Exception ex) { UiHelper.ShowErrorDialog("Fehler", ex.Message); }
            finally { _btnShareUser.Enabled = true; }
        }

        private Label CreateHeaderLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent
            };
        }
    }
}