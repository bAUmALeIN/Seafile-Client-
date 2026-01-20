using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp3.Data;
using System.Drawing.Imaging; // Wichtig für Bild-Bearbeitung

namespace WinFormsApp3
{
    public partial class FrmAbout : MaterialForm
    {
        public FrmAbout()
        {
            // WICHTIG: Das Form muss beim Manager angemeldet werden, 
            // sonst wird der Hintergrund nicht dunkel gefärbt!
            MaterialSkinManager.Instance.AddFormToManage(this);

            InitializeComponentUI();
            LoadDebugInfo();
        }

        private void InitializeComponentUI()
        {
            this.Size = new Size(550, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Über & Debug";
            this.Sizable = false;

            // 1. Großes Logo (Mit "Fuzzy"-Transparenz für unsaubere Ränder)
            PictureBox pbLogo = new PictureBox();
            pbLogo.Size = new Size(130, 130);
            pbLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pbLogo.Location = new Point((this.Width - pbLogo.Width) / 2, 80);

            try
            {
                // Original laden
                Bitmap original = new Bitmap(Properties.Resources.app_logo);
                // Intelligente Transparenz anwenden (entfernt auch hellgraue Pixel)
                pbLogo.Image = RemoveWhiteBackground(original);
            }
            catch
            {
                pbLogo.Image = Properties.Resources.icon_repo;
            }
            this.Controls.Add(pbLogo);

            // 2. Banner (Blau hinterlegt)
            Label lblTitle = new Label();
            lblTitle.Text = "BBS-ME Seafile Client";
            lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;

            // Holt sich das exakte Blau vom aktuellen Theme
            lblTitle.BackColor = MaterialSkinManager.Instance.ColorScheme.PrimaryColor;

            lblTitle.AutoSize = false;
            lblTitle.Size = new Size(300, 40);
            lblTitle.Location = new Point((this.Width - 300) / 2, pbLogo.Bottom + 15);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // 3. Version (Hintergrund Transparent machen!)
            Label lblVer = new Label();
            lblVer.Text = "Version v1.5 (Unofficial)";
            lblVer.Font = new Font("Segoe UI", 10f);
            lblVer.ForeColor = Color.Gray;
            // FIX: Damit scheint der dunkle Hintergrund durch
            lblVer.BackColor = Color.Transparent;

            lblVer.AutoSize = false;
            lblVer.Size = new Size(this.Width, 25);
            lblVer.TextAlign = ContentAlignment.MiddleCenter;
            lblVer.Location = new Point(0, lblTitle.Bottom + 5);
            this.Controls.Add(lblVer);

            // 4. Debug Box
            RichTextBox rtbDebug = new RichTextBox();
            rtbDebug.Name = "rtbDebug";
            rtbDebug.BackColor = Color.FromArgb(40, 40, 40);
            rtbDebug.BorderStyle = BorderStyle.None;
            rtbDebug.Location = new Point(40, lblVer.Bottom + 20);
            rtbDebug.Size = new Size(470, 280);
            rtbDebug.ReadOnly = true;
            rtbDebug.Cursor = Cursors.Default;
            rtbDebug.ScrollBars = RichTextBoxScrollBars.None;
            rtbDebug.Font = new Font("Consolas", 9f);
            this.Controls.Add(rtbDebug);

            // 5. Close Button
            MaterialButton btnClose = new MaterialButton();
            btnClose.Text = "SCHLIESSEN";
            btnClose.AutoSize = false;
            btnClose.Size = new Size(200, 36);
            btnClose.Location = new Point((this.Width - 200) / 2, rtbDebug.Bottom + 20);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        // --- NEU: INTELLIGENTE TRANSPARENZ ---
        // Entfernt alles was weiß oder fast weiß ist (Threshold)
        private Bitmap RemoveWhiteBackground(Bitmap bmp)
        {
            Bitmap result = new Bitmap(bmp.Width, bmp.Height);
            // Toleranz-Wert: Je niedriger, desto strenger (230 ist gut für JPG-Artefakte)
            int threshold = 230;

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    // Prüfen ob der Pixel hell ist (R, G und B > Threshold)
                    if (pixel.R > threshold && pixel.G > threshold && pixel.B > threshold)
                    {
                        // Mach ihn transparent
                        result.SetPixel(x, y, Color.Transparent);
                    }
                    else
                    {
                        // Behalte Originalfarbe
                        result.SetPixel(x, y, pixel);
                    }
                }
            }
            return result;
        }

        private void LoadDebugInfo()
        {
            RichTextBox rtb = this.Controls["rtbDebug"] as RichTextBox;
            if (rtb == null) return;

            rtb.Clear();

            // Checks
            string dbPath = Path.Combine(Application.StartupPath, "seafile_data.db");
            bool dbExists = File.Exists(dbPath);
            string tokenStatus = "❌ Nicht gefunden";
            if (dbExists)
            {
                var db = new DBHelper();
                string t = db.GetToken();
                if (!string.IsNullOrEmpty(t)) tokenStatus = "✅ Vorhanden (" + t.Substring(0, 5) + "...)";
            }

            string cachePath = Path.Combine(Application.StartupPath, AppConfig.WebViewUserDataFolder);
            long cacheSize = 0;
            bool cacheExists = Directory.Exists(cachePath);
            if (cacheExists)
            {
                try { cacheSize = new DirectoryInfo(cachePath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length); } catch { }
            }
            string cookieStatus = (cacheExists && cacheSize > 0) ? $"✅ Cache ({UiHelper.FormatByteSize(cacheSize)})" : "⚠️ Leer";

            // Output
            AppendHeader(rtb, "--- SYSTEM INFO ---");
            AppendKeyValue(rtb, "OS", Environment.OSVersion.ToString());
            AppendKeyValue(rtb, "User", Environment.UserName);
            AppendKeyValue(rtb, "Machine", Environment.MachineName);
            rtb.AppendText("\n");

            AppendHeader(rtb, "--- APP CONFIG ---");
            AppendKeyValue(rtb, "API URL", AppConfig.ApiBaseUrl);
            AppendKeyValue(rtb, "Login URL", AppConfig.LoginUrl);
            rtb.AppendText("\n");

            AppendHeader(rtb, "--- STATUS ---");
            AppendKeyValue(rtb, "API Token", tokenStatus, tokenStatus.Contains("✅") ? Color.LightGreen : Color.Salmon);
            AppendKeyValue(rtb, "Browser Session", cookieStatus, cookieStatus.Contains("✅") ? Color.LightGreen : Color.Orange);

            if (string.IsNullOrEmpty(AppConfig.CSRFToken)) AppendKeyValue(rtb, "CSRF (RAM)", "Leer (Wird bei Bedarf geladen)", Color.Gray);
            else AppendKeyValue(rtb, "CSRF (RAM)", "Aktiv", Color.LightGreen);

            rtb.AppendText("\n");

            AppendHeader(rtb, "--- SPEICHERORTE ---");
            AppendKeyValue(rtb, "DB", dbPath, Color.Gray);
            AppendKeyValue(rtb, "Cache", cachePath, Color.Gray);
        }

        private void AppendHeader(RichTextBox box, string text)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = Color.FromArgb(100, 181, 246);
            box.SelectionFont = new Font("Consolas", 9f, FontStyle.Bold);
            box.AppendText(text + "\n");
            box.SelectionFont = new Font("Consolas", 9f, FontStyle.Regular);
        }

        private void AppendKeyValue(RichTextBox box, string key, string value, Color? valueColor = null)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = Color.Gray;
            box.AppendText(key.PadRight(16) + ": ");

            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = valueColor ?? Color.WhiteSmoke;
            box.AppendText(value + "\n");
        }
    }
}