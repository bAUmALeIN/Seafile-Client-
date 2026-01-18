using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class FrmTransferDetail : MaterialForm
    {
        private readonly DownloadItem _item;
        private RichTextBox rtbInfo;
        private MaterialButton btnClose;
        private PictureBox picStatus;

        public FrmTransferDetail(DownloadItem item)
        {
            _item = item;
            InitializeComponent();
            ApplyDynamicStatusTheme();
            PopulateData();

            rtbInfo.GotFocus += (s, e) => { NativeMethods.HideCaret(rtbInfo.Handle); };
            rtbInfo.Cursor = Cursors.Default;
        }

        private void InitializeComponent()
        {
            picStatus = new PictureBox();
            rtbInfo = new RichTextBox();
            btnClose = new MaterialButton();
            ((System.ComponentModel.ISupportInitialize)picStatus).BeginInit();
            SuspendLayout();

            picStatus.Location = new Point(190, 80);
            picStatus.Name = "picStatus";
            picStatus.Size = new Size(120, 120);
            picStatus.SizeMode = PictureBoxSizeMode.Zoom;
            picStatus.TabIndex = 0;
            picStatus.TabStop = false;

            rtbInfo.BackColor = Color.FromArgb(50, 50, 50);
            rtbInfo.BorderStyle = BorderStyle.None;
            rtbInfo.Font = new Font("Segoe UI", 11F);
            rtbInfo.ForeColor = Color.White;
            rtbInfo.Location = new Point(30, 220);
            rtbInfo.Name = "rtbInfo";
            rtbInfo.ReadOnly = true;
            rtbInfo.Size = new Size(440, 180);
            rtbInfo.TabIndex = 1;
            rtbInfo.Text = "";

            btnClose.AutoSize = false;
            btnClose.Depth = 0;
            btnClose.HighEmphasis = true;
            btnClose.Location = new Point(340, 420);
            btnClose.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(130, 40);
            btnClose.TabIndex = 2;
            btnClose.Text = "SCHLIESSEN";
            btnClose.Type = MaterialButton.MaterialButtonType.Contained;
            btnClose.UseAccentColor = false;
            btnClose.Click += (s, e) => this.Close();

            ClientSize = new Size(500, 480);
            Controls.Add(picStatus);
            Controls.Add(rtbInfo);
            Controls.Add(btnClose);
            Name = "FrmTransferDetail";
            Sizable = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Transfer Details";

            ((System.ComponentModel.ISupportInitialize)picStatus).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void PopulateData()
        {
            string cleanName = _item.FileName.Replace("⬇", "").Replace("⬆", "").Trim();

            rtbInfo.Clear();

            // Helper function to append text with color
            void Append(string text, Color color, bool bold = false)
            {
                rtbInfo.SelectionStart = rtbInfo.TextLength;
                rtbInfo.SelectionLength = 0;
                rtbInfo.SelectionColor = color;
                rtbInfo.SelectionFont = new Font(rtbInfo.Font, bold ? FontStyle.Bold : FontStyle.Regular);
                rtbInfo.AppendText(text + "\n");
            }

            Append($"Datei:      {cleanName}", Color.White);
            Append($"Richtung:   {_item.Type}", Color.Gray);
            Append($"Status:     {_item.Status}", _item.Status.Contains("Fehler") ? Color.Salmon : Color.LightGreen);
            Append($"Fortschritt: {_item.Progress}%", Color.White);
            Append($"Startzeit:  {_item.StartTime}", Color.White);
            Append("", Color.White);
            Append($"ID: {_item.Id}", Color.DarkGray);

            // NEU: Fehler anzeigen wenn vorhanden
            if (!string.IsNullOrEmpty(_item.ErrorMessage))
            {
                Append("", Color.White);
                Append("FEHLERMELDUNG:", Color.Red, true);
                Append(_item.ErrorMessage, Color.Orange);
            }
        }

        private void ApplyDynamicStatusTheme()
        {
            var skinManager = MaterialSkinManager.Instance;
            var oldScheme = skinManager.ColorScheme;

            if (_item.Status.Contains("Fehler"))
            {
                skinManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Red600, MaterialPrimary.Red800, MaterialPrimary.Red200, MaterialAccent.Red200, MaterialTextShade.WHITE);
                picStatus.Image = Properties.Resources.Status_error;
            }
            else if (_item.Status == "Fertig")
            {
                skinManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Green600, MaterialPrimary.Green800, MaterialPrimary.Green200, MaterialAccent.LightGreen200, MaterialTextShade.WHITE);
                picStatus.Image = Properties.Resources.Status_ok;
            }
            else
            {
                skinManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Blue600, MaterialPrimary.Blue800, MaterialPrimary.Blue200, MaterialAccent.LightBlue200, MaterialTextShade.WHITE);
                picStatus.Image = Properties.Resources.icon_warten;
            }
            this.FormClosed += (s, e) => skinManager.ColorScheme = oldScheme;
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool HideCaret(IntPtr hWnd);
        }
    }
}