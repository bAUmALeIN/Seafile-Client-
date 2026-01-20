using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
        private System.Windows.Forms.ListView _lstDetails;
        private ImageList _detailIcons; // NEU: Speicher für die Icons

        // Logic
        private System.Windows.Forms.Timer _animTimer;
        private System.Windows.Forms.Timer _refreshTimer;
        private float _currentAngle = 0f;
        private Image _loadingImageOriginal;
        private MaterialColorScheme _defaultScheme;

        public FrmTransferDetail(DownloadItem item)
        {
            _item = item;
            _defaultScheme = MaterialSkinManager.Instance.ColorScheme;

            InitializeComponent();
            InitializeStaticLayout();

            SetupAnimation();
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _refreshTimer.Tick += (s, e) => UpdateUI();
            _refreshTimer.Start();

            UpdateUI();
            this.FormClosing += FrmTransferDetail_FormClosing;
        }

        private void FrmTransferDetail_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopAllTimers();
            MaterialSkinManager.Instance.ColorScheme = _defaultScheme;
        }

        private void StopAllTimers()
        {
            if (_animTimer != null && _animTimer.Enabled) _animTimer.Stop();
            if (_refreshTimer != null && _refreshTimer.Enabled) _refreshTimer.Stop();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(600, 700);
            this.Sizable = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Transfer Details";

            picStatus = new PictureBox();
            picStatus.Size = new Size(80, 80);
            picStatus.SizeMode = PictureBoxSizeMode.CenterImage;
            picStatus.Location = new Point((this.ClientSize.Width - picStatus.Width) / 2, 80);
            picStatus.BackColor = Color.Transparent;
            this.Controls.Add(picStatus);

            lblHeader = new Label();
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.White;
            lblHeader.BackColor = Color.Transparent;
            lblHeader.AutoSize = false;
            lblHeader.TextAlign = ContentAlignment.MiddleCenter;
            lblHeader.Size = new Size(550, 30);
            lblHeader.Location = new Point(25, picStatus.Bottom + 10);
            this.Controls.Add(lblHeader);

            MaterialButton btnClose = new MaterialButton();
            btnClose.Text = "SCHLIESSEN";
            btnClose.Type = MaterialButton.MaterialButtonType.Contained;
            btnClose.UseAccentColor = false;
            btnClose.AutoSize = false;
            btnClose.Size = new Size(130, 36);
            btnClose.Location = new Point(440, 640);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            MaterialButton btnCopy = new MaterialButton();
            btnCopy.Text = "KOPIEREN";
            btnCopy.Type = MaterialButton.MaterialButtonType.Outlined;
            btnCopy.AutoSize = false;
            btnCopy.Size = new Size(130, 36);
            btnCopy.Location = new Point(300, 640);
            btnCopy.Click += BtnCopy_Click;
            this.Controls.Add(btnCopy);
        }

        private void InitializeStaticLayout()
        {
            FlowLayoutPanel flowContent = new FlowLayoutPanel();
            flowContent.BackColor = Color.FromArgb(45, 45, 48);
            flowContent.Location = new Point(30, lblHeader.Bottom + 10);
            flowContent.Size = new Size(540, 200);
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
            lblValRemotePath = AddGridRow(tlp, "Server Pfad", Color.Orange);
            lblValStatus = AddGridRow(tlp, "Status", Color.White);
            lblValRate = AddGridRow(tlp, "Rate", Color.Cyan);
            lblValProgress = AddGridRow(tlp, "Progress", Color.White);
            lblValStart = AddGridRow(tlp, "Gestartet", Color.White);

            flowContent.Controls.Add(tlp);

            lblMsg = new Label();
            lblMsg.AutoSize = true;
            lblMsg.Margin = new Padding(0, 10, 0, 0);
            lblMsg.Font = new Font("Consolas", 9F);
            lblMsg.ForeColor = Color.LightGray;
            flowContent.Controls.Add(lblMsg);

            if (_item.SubItems != null && _item.SubItems.Count > 0)
            {
                Label lblDetails = new Label
                {
                    Text = "Enthaltene Dateien:",
                    ForeColor = Color.Gray,
                    BackColor = Color.Transparent,
                    Location = new Point(30, flowContent.Bottom + 10),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                };
                this.Controls.Add(lblDetails);

                // NEU: ImageList erstellen
                _detailIcons = new ImageList();
                _detailIcons.ColorDepth = ColorDepth.Depth32Bit;
                _detailIcons.ImageSize = new Size(16, 16); // Kleine Icons für die Liste

                _lstDetails = new System.Windows.Forms.ListView();
                _lstDetails.Location = new Point(30, lblDetails.Bottom + 5);
                _lstDetails.Size = new Size(540, 200);
                _lstDetails.View = View.Details;
                _lstDetails.BackColor = Color.FromArgb(40, 40, 40);
                _lstDetails.ForeColor = Color.WhiteSmoke;
                _lstDetails.BorderStyle = BorderStyle.FixedSingle;
                _lstDetails.SmallImageList = _detailIcons; // NEU: Icons verknüpfen

                _lstDetails.Columns.Add("Datei", 350);
                _lstDetails.Columns.Add("Status", 150);
                _lstDetails.HeaderStyle = ColumnHeaderStyle.Nonclickable;

                _lstDetails.BeginUpdate();
                foreach (var sub in _item.SubItems)
                {
                    // NEU: Icon Logik
                    string ext = Path.GetExtension(sub.Name).ToLower();
                    if (!_detailIcons.Images.ContainsKey(ext))
                    {
                        // Versuche System-Icon zu holen
                        Icon sysIcon = IconHelper.GetIconForExtension(ext, false);
                        if (sysIcon != null) _detailIcons.Images.Add(ext, sysIcon);
                        else _detailIcons.Images.Add(ext, Properties.Resources.icon_file); // Fallback
                    }

                    var lvi = new ListViewItem(sub.Name);
                    lvi.ImageKey = ext; // Icon setzen
                    lvi.SubItems.Add(sub.Status);
                    _lstDetails.Items.Add(lvi);
                }
                _lstDetails.EndUpdate();

                this.Controls.Add(_lstDetails);
            }
        }

        private Label AddGridRow(TableLayoutPanel tlp, string key, Color valColor)
        {
            Label k = new Label { Text = key + ":", ForeColor = Color.Gray, Font = new Font("Segoe UI", 9F), AutoSize = true, Margin = new Padding(0, 0, 0, 3) };
            Label v = new Label { Text = "-", ForeColor = valColor, Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 3) };
            tlp.RowCount++;
            tlp.Controls.Add(k, 0, tlp.RowCount - 1);
            tlp.Controls.Add(v, 1, tlp.RowCount - 1);
            return v;
        }

        private void UpdateUI()
        {
            string cleanName = _item.FileName.Replace("⬇", "").Replace("⬆", "").Trim();
            if (lblHeader.Text != cleanName) lblHeader.Text = ShortenString(cleanName, 50);

            lblValType.Text = _item.Type;
            lblValRemotePath.Text = _item.RemotePath ?? "-";
            lblValStatus.Text = _item.Status;
            lblValStatus.ForeColor = GetStatusColor(_item.Status);
            lblValRate.Text = _item.SpeedString;
            lblValProgress.Text = $"{_item.Progress}%";
            lblValStart.Text = _item.StartTime.ToString("T");

            if (!string.IsNullOrEmpty(_item.ErrorMessage))
            {
                lblMsg.ForeColor = Color.Orange;
                lblMsg.Text = _item.ErrorMessage;
                UpdateStatusTheme("Error");
            }
            else if (_item.Status == "Fertig")
            {
                lblMsg.ForeColor = Color.LightGreen;
                lblMsg.Text = "Fertig.";
                UpdateStatusTheme("Success");
                StopAllTimers();

                if (picStatus.Image != null)
                {
                    Bitmap transparentBitmap = new Bitmap(picStatus.Image);
                    transparentBitmap.MakeTransparent(Color.White);
                    picStatus.Image = transparentBitmap;
                }
            }
            else
            {
                lblMsg.Text = "";
                if (!_animTimer.Enabled) _animTimer.Start();
            }

            if (_lstDetails != null && _item.SubItems != null)
            {
                _lstDetails.BeginUpdate();
                for (int i = 0; i < _item.SubItems.Count; i++)
                {
                    if (i < _lstDetails.Items.Count)
                    {
                        var sub = _item.SubItems[i];
                        var subItem = _lstDetails.Items[i].SubItems[1];

                        if (subItem.Text != sub.Status)
                        {
                            subItem.Text = sub.Status;
                        }

                        if (sub.Status.Contains("Fertig")) _lstDetails.Items[i].ForeColor = Color.LightGreen;
                        else if (sub.Status.Contains("Fehler")) _lstDetails.Items[i].ForeColor = Color.Salmon;
                        else _lstDetails.Items[i].ForeColor = Color.WhiteSmoke;
                    }
                }
                _lstDetails.EndUpdate();
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
            string content = $"File: {_item.FileName}\nID: {_item.Id}";
            Clipboard.SetText(content);
            new MaterialSnackBar("Kopiert", 1500).Show(this);
        }

        private string ShortenString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}