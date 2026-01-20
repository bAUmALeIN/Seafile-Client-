using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using System;
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

        private MaterialTextBoxEdit _txtLink;
        private MaterialButton _btnCopy;
        private MaterialButton _btnCreate;
        private Label _lblStatus;

        public FrmShare(SeafileClient client, string repoId, string path, string itemName, bool isDir)
        {
            _client = client;
            _repoId = repoId;
            _path = path;
            _itemName = itemName;
            _isDir = isDir;

            MaterialSkinManager.Instance.AddFormToManage(this);
            InitializeComponentUI();
        }

        private void InitializeComponentUI()
        {
            this.Size = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Freigabe erstellen";
            this.Sizable = false;

            // Info Label
            Label lblInfo = new Label();
            string typeStr = _isDir ? "den Ordner" : "die Datei";
            lblInfo.Text = $"Erstelle einen öffentlichen Link für {typeStr}:\n'{_itemName}'";
            lblInfo.Font = new Font("Segoe UI", 11f);
            lblInfo.ForeColor = Color.White;
            lblInfo.Location = new Point(20, 80);
            lblInfo.AutoSize = true;
            this.Controls.Add(lblInfo);

            // TextBox für den Link (ReadOnly)
            _txtLink = new MaterialTextBoxEdit();
            _txtLink.Hint = "Generierter Link";
            _txtLink.ReadOnly = true;
            _txtLink.Location = new Point(20, 140);
            _txtLink.Size = new Size(350, 50);
            this.Controls.Add(_txtLink);

            // Copy Button (klein daneben)
            _btnCopy = new MaterialButton();
            _btnCopy.Text = "KOPIEREN";
            _btnCopy.Type = MaterialButton.MaterialButtonType.Outlined;
            _btnCopy.Location = new Point(380, 148);
            _btnCopy.Size = new Size(100, 36);
            _btnCopy.Enabled = false; // Erst aktiv wenn Link da
            _btnCopy.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_txtLink.Text))
                {
                    Clipboard.SetText(_txtLink.Text);
                    _lblStatus.Text = "Link in Zwischenablage kopiert!";
                    _lblStatus.ForeColor = Color.LightGreen;
                }
            };
            this.Controls.Add(_btnCopy);

            // Create Button (unten)
            _btnCreate = new MaterialButton();
            _btnCreate.Text = "LINK ERSTELLEN";
            _btnCreate.Type = MaterialButton.MaterialButtonType.Contained;
            _btnCreate.UseAccentColor = false;
            _btnCreate.Location = new Point(20, 220);
            _btnCreate.Size = new Size(460, 40);
            _btnCreate.Click += async (s, e) => await CreateLink();
            this.Controls.Add(_btnCreate);

            // Status Label
            _lblStatus = new Label();
            _lblStatus.Text = "";
            _lblStatus.ForeColor = Color.Orange;
            _lblStatus.Location = new Point(20, 270);
            _lblStatus.AutoSize = true;
            this.Controls.Add(_lblStatus);
        }

        private async Task CreateLink()
        {
            try
            {
                _btnCreate.Enabled = false;
                _btnCreate.Text = "Erstelle...";
                _lblStatus.Text = "Kommuniziere mit Server...";
                _lblStatus.ForeColor = Color.LightBlue;

                string link = await _client.CreateShareLinkAsync(_repoId, _path);

                _txtLink.Text = link;
                _btnCopy.Enabled = true;
                _btnCreate.Text = "NEUEN LINK ERSTELLEN"; // Falls man resetten will (API erlaubt mehrere, hier vereinfacht)

                _lblStatus.Text = "Link erfolgreich erstellt!";
                _lblStatus.ForeColor = Color.LightGreen;
            }
            catch (Exception ex)
            {
                UiHelper.ShowErrorDialog("Fehler", ex.Message);
                _lblStatus.Text = "Fehler beim Erstellen.";
                _lblStatus.ForeColor = Color.Salmon;
            }
            finally
            {
                _btnCreate.Enabled = true;
                if (_btnCreate.Text == "Erstelle...") _btnCreate.Text = "LINK ERSTELLEN";
            }
        }
    }
}