using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinFormsApp3.Data; // Wichtig für AppConfig

namespace WinFormsApp3
{
    public partial class FrmPreview : MaterialForm
    {
        private readonly string _target; // Pfad oder URL
        private readonly string _fileName;
        private readonly bool _isWebUrl;
        private WebView2 _webView;

        public FrmPreview(string target, string fileName, bool isWebUrl)
        {
            _target = target;
            _fileName = fileName;
            _isWebUrl = isWebUrl;

            InitializeComponentUI();
            SetupWebView();
        }

        private void InitializeComponentUI()
        {
            this.Size = new Size(1200, 900); // Etwas größer für Office
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Vorschau: " + _fileName;

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = Color.FromArgb(30, 30, 30);

            System.Windows.Forms.Panel container = new System.Windows.Forms.Panel();
            container.Dock = DockStyle.Fill;
            container.Padding = new Padding(3, 64, 3, 3);
            container.Controls.Add(_webView);

            this.Controls.Add(container);
        }

        private async void SetupWebView()
        {
            // CRITICAL: Wir müssen denselben UserDataFolder nutzen wie beim Login,
            // damit die Cookies (Session) da sind! Sonst müsste man sich neu einloggen.
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Application.StartupPath, AppConfig.WebViewUserDataFolder));

            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            if (_isWebUrl)
            {
                // Web-Modus (für Office Docs via Seafile Server)
                _webView.Source = new Uri(_target);
            }
            else
            {
                // Lokaler Modus (Bilder, PDFs)
                if (File.Exists(_target))
                {
                    _webView.Source = new Uri(_target);
                }
                else
                {
                    MessageBox.Show("Datei nicht gefunden:\n" + _target);
                    this.Close();
                }
            }
        }
    }
}