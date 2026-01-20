using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class FrmTransferDetail : MaterialForm
    {
        private readonly DownloadItem _item;
        // UI Controls
        private Label lblValType, lblValStatus, lblValRate, lblValProgress, lblValStart, lblMsg, lblValRemotePath;
        private PictureBox picStatus;
        private Label lblHeader;

        // Logic
        private System.Windows.Forms.Timer _animTimer;
        private System.Windows.Forms.Timer _refreshTimer;
        private float _currentAngle = 0f;
        private Image _loadingImageOriginal;
        // Theme Backup
        private MaterialColorScheme _defaultScheme;

        public FrmTransferDetail(DownloadItem item)
        {
            _item = item;
            // Theme sichern
            _defaultScheme = MaterialSkinManager.Instance.ColorScheme;

            InitializeComponent();
            InitializeStaticLayout();

            SetupAnimation();
            // Timer starten (200ms = 5 FPS für Text-Updates reicht völlig)
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _refreshTimer.Tick += (s, e) => UpdateUI();
            _refreshTimer.Start();

            // Erstes Update sofort erzwingen
            UpdateUI();
            this.FormClosing += FrmTransferDetail_FormClosing;
        }

        private void FrmTransferDetail_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopAllTimers();
            // Theme zurücksetzen beim Schließen
            MaterialSkinManager.Instance.ColorScheme = _defaultScheme;
        }

        private void StopAllTimers()
        {
            if (_animTimer != null && _animTimer.Enabled) _animTimer.Stop();
            if (_refreshTimer != null && _refreshTimer.Enabled) _refreshTimer.Stop();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(550, 580); // Etwas höher für den Pfad
            this.Sizable = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Transfer Details";

            picStatus = new PictureBox();
            picStatus.Size = new Size(100, 100);
            picStatus.SizeMode = PictureBoxSizeMode.CenterImage;
            picStatus.Location = new Point((this.ClientSize.Width - picStatus.Width) / 2, 80);
            picStatus.BackColor = Color.Transparent;
            this.Controls.Add(picStatus);

            lblHeader = new Label();
            lblHeader.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblHeader.ForeColor = Color.White;
            lblHeader.BackColor = Color.Transparent;
            lblHeader.AutoSize = false;
            lblHeader.TextAlign = ContentAlignment.MiddleCenter;
            lblHeader.Size = new Size(500, 30);
            lblHeader.Location = new Point(25, picStatus.Bottom + 15);
            this.Controls.Add(lblHeader);

            MaterialButton btnClose = new MaterialButton();
            btnClose.Text = "SCHLIESSEN";
            btnClose.Type = MaterialButton.MaterialButtonType.Contained;
            btnClose.UseAccentColor = false;
            btnClose.AutoSize = false;
            btnClose.Size = new Size(130, 36);
            btnClose.Location = new Point(390, 510);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            MaterialButton btnCopy = new MaterialButton();
            btnCopy.Text = "KOPIEREN";
            btnCopy.Type = MaterialButton.MaterialButtonType.Outlined;
            btnCopy.AutoSize = false;
            btnCopy.Size = new Size(130, 36);
            btnCopy.Location = new Point(250, 510);
            btnCopy.Click += BtnCopy_Click;
            this.Controls.Add(btnCopy);
        }

        private void InitializeStaticLayout()
        {
            FlowLayoutPanel flowContent = new FlowLayoutPanel();
            flowContent.BackColor = Color.FromArgb(45, 45, 48);
            flowContent.Location = new Point(30, lblHeader.Bottom + 20);
            flowContent.Size = new Size(490, 250);
            flowContent.FlowDirection = FlowDirection.TopDown;
            flowContent.WrapContents = false;
            flowContent.AutoScroll = true;
            flowContent.Padding = new Padding(15);
            this.Controls.Add(flowContent);

            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.AutoSize = true;
            tlp.ColumnCount = 2;
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblValType = AddGridRow(tlp, "Typ", Color.White);

            // NEU: Remote Pfad
            lblValRemotePath = AddGridRow(tlp, "Server Pfad", Color.Orange);

            lblValStatus = AddGridRow(tlp, "Status", Color.White);
            lblValRate = AddGridRow(tlp, "Rate", Color.Cyan);
            lblValProgress = AddGridRow(tlp, "Progress", Color.White);
            lblValStart = AddGridRow(tlp, "Gestartet", Color.White);

            flowContent.Controls.Add(tlp);
            System.Windows.Forms.Panel line = new System.Windows.Forms.Panel();
            line.Height = 1; line.BackColor = Color.Gray;
            line.Width = flowContent.ClientSize.Width - 40;
            line.Margin = new Padding(0, 15, 0, 15);
            flowContent.Controls.Add(line);

            lblMsg = new Label();
            lblMsg.AutoSize = true;
            lblMsg.MaximumSize = new Size(flowContent.ClientSize.Width - 30, 0);
            lblMsg.Font = new Font("Consolas", 10F);
            lblMsg.Text = "Initialisiere...";
            flowContent.Controls.Add(lblMsg);
        }

        private Label AddGridRow(TableLayoutPanel tlp, string key, Color valColor)
        {
            Label k = new Label { Text = key + ":", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10F), AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
            Label v = new Label { Text = "-", ForeColor = valColor, Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
            tlp.RowCount++;
            tlp.Controls.Add(k, 0, tlp.RowCount - 1);
            tlp.Controls.Add(v, 1, tlp.RowCount - 1);
            return v;
        }

        // --- CORE UPDATE LOGIC ---
        private void UpdateUI()
        {
            // 1. Daten in UI schreiben
            string cleanName = _item.FileName.Replace("⬇", "").Replace("⬆", "").Trim();
            if (lblHeader.Text != cleanName) lblHeader.Text = ShortenString(cleanName, 40);

            lblValType.Text = _item.Type;

            // Update Pfad
            lblValRemotePath.Text = _item.RemotePath ?? "-";

            lblValStatus.Text = _item.Status;
            lblValStatus.ForeColor = GetStatusColor(_item.Status);
            lblValRate.Text = _item.SpeedString;
            lblValProgress.Text = $"{_item.Progress}%";
            lblValStart.Text = _item.StartTime.ToString("T");

            // 2. Status prüfen & Theme setzen
            bool isFinished = false;
            if (!string.IsNullOrEmpty(_item.ErrorMessage))
            {
                lblMsg.ForeColor = Color.Orange;
                lblMsg.Text = "FEHLER:\n" + _item.ErrorMessage;
                UpdateStatusTheme("Error");
                isFinished = true;
            }
            else if (_item.Status == "Fertig")
            {
                lblMsg.ForeColor = Color.LightGreen;
                lblMsg.Text = "Transfer erfolgreich abgeschlossen.";
                UpdateStatusTheme("Success");
                isFinished = true;
            }
            else
            {
                lblMsg.ForeColor = Color.LightBlue;
                lblMsg.Text = "Übertragung läuft...\nBitte das Fenster nicht schließen.";
                if (!_animTimer.Enabled) _animTimer.Start();
            }

            // 3. KILL SWITCH: Wenn fertig, Timer stoppen um CPU zu sparen
            if (isFinished)
            {
                StopAllTimers();
                if (picStatus.Image != null)
                {
                    Bitmap transparentBitmap = new Bitmap(picStatus.Image);
                    transparentBitmap.MakeTransparent(Color.White);
                    picStatus.Image = transparentBitmap;
                }
            }
        }

        private Color GetStatusColor(string status)
        {
            if (status.Contains("Fehler") || status == "Abgebrochen") return Color.Salmon;
            if (status == "Fertig") return Color.LightGreen;
            return Color.LightBlue;
        }

        private string _lastThemeStatus = "";
        private void UpdateStatusTheme(string statusType)
        {
            if (_lastThemeStatus == statusType) return;
            _lastThemeStatus = statusType;

            var skinManager = MaterialSkinManager.Instance;

            if (statusType == "Error")
            {
                skinManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Red600, MaterialPrimary.Red800, MaterialPrimary.Red200, MaterialAccent.Red200, MaterialTextShade.WHITE);
                picStatus.Image = Properties.Resources.Status_error;
            }
            else if (statusType == "Success")
            {
                skinManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Green600, MaterialPrimary.Green800, MaterialPrimary.Green200, MaterialAccent.LightGreen200, MaterialTextShade.WHITE);
                picStatus.Image = Properties.Resources.Status_ok;
            }
        }

        private void SetupAnimation()
        {
            _loadingImageOriginal = Properties.Resources.icon_warten;
            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 50;
            _animTimer.Tick += (s, e) =>
            {
                _currentAngle += 15f;
                if (_currentAngle >= 360f) _currentAngle = 0f;
                picStatus.Image = RotateImage(_loadingImageOriginal, _currentAngle);
            };
        }

        private Image RotateImage(Image img, float angle)
        {
            Bitmap rotatedBmp = new Bitmap(img.Width, img.Height);
            rotatedBmp.SetResolution(img.HorizontalResolution, img.VerticalResolution);
            using (Graphics g = Graphics.FromImage(rotatedBmp))
            {
                g.TranslateTransform(img.Width / 2, img.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-img.Width / 2, -img.Height / 2);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, new Point(0, 0));
            }
            return rotatedBmp;
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            string content = $"File: {_item.FileName}\nStatus: {_item.Status}\nRate: {_item.SpeedString}\nError: {_item.ErrorMessage ?? "-"}\nID: {_item.Id}\nPfad: {_item.RemotePath}";
            Clipboard.SetText(content);
            new MaterialSnackBar("In die Zwischenablage kopiert", 1500).Show(this);
        }

        private string ShortenString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}