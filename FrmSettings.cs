using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Util;
using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class FrmSettings : MaterialForm
    {
        private MaterialTextBoxEdit txtApiUrl;
        private MaterialTextBoxEdit txtLoginUrl;
        private MaterialButton btnSave;
        private MaterialButton btnCancel;

        // Variable zum Speichern des alten Farbschemas
        private MaterialColorScheme _oldScheme;

        public FrmSettings()
        {
            InitializeComponent();
            InitializeCustomUI();
            LoadSettings();
        }

        private void InitializeCustomUI()
        {
            this.Text = "Einstellungen";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Sizable = false;

            var skin = MaterialSkinManager.Instance;
            skin.AddFormToManage(this);

            // Farbschema speichern und anpassen
            _oldScheme = skin.ColorScheme;
            skin.ColorScheme = new MaterialColorScheme(
                MaterialPrimary.Blue600, MaterialPrimary.Blue800, MaterialPrimary.Blue200,
                MaterialAccent.Red400, MaterialTextShade.WHITE
            );

            // Controls
            txtApiUrl = new MaterialTextBoxEdit();
            txtApiUrl.Hint = "API Basis URL";
            txtApiUrl.Location = new Point(20, 90);
            txtApiUrl.Size = new Size(460, 50);
            this.Controls.Add(txtApiUrl);

            txtLoginUrl = new MaterialTextBoxEdit();
            txtLoginUrl.Hint = "Login Web URL";
            txtLoginUrl.Location = new Point(20, 160);
            txtLoginUrl.Size = new Size(460, 50);
            this.Controls.Add(txtLoginUrl);

            // SAVE BUTTON (BLAU)
            btnSave = new MaterialButton();
            btnSave.Text = "SPEICHERN & NEUSTART";
            btnSave.Location = new Point(250, 300);
            btnSave.Size = new Size(230, 36);
            btnSave.Type = MaterialButton.MaterialButtonType.Contained;
            btnSave.UseAccentColor = false;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // CANCEL BUTTON (ROT)
            btnCancel = new MaterialButton();
            btnCancel.Text = "ABBRECHEN";
            btnCancel.Location = new Point(110, 300);
            btnCancel.Type = MaterialButton.MaterialButtonType.Contained;
            btnCancel.UseAccentColor = true;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            // Schema beim Schließen wiederherstellen
            this.FormClosed += (s, e) =>
            {
                skin.ColorScheme = _oldScheme;
            };
        }

        private void LoadSettings()
        {
            txtApiUrl.Text = AppConfig.ApiBaseUrl;
            txtLoginUrl.Text = AppConfig.LoginUrl;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtApiUrl.Text)) return;

            var db = new DBHelper();
            db.SaveSetting(AppConfig.SettingsKeys.ApiUrl, txtApiUrl.Text.Trim());
            db.SaveSetting(AppConfig.SettingsKeys.LoginUrl, txtLoginUrl.Text.Trim());

            // Nutzung des UiHelper statt MessageBox
            UiHelper.ShowSuccessDialog("Info", "Einstellungen gespeichert. Die Anwendung wird neu gestartet.");

            MaterialSkinManager.Instance.ColorScheme = _oldScheme;
            Application.Restart();
            Environment.Exit(0);
        }
    }
}