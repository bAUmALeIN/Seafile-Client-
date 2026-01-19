using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class AuthManager
    {
        private readonly WebView2 _webView;
        private readonly string _userDataFolder;

        public AuthManager(WebView2 webView)
        {
            _webView = webView;
            _userDataFolder = Path.Combine(Application.StartupPath, AppConfig.WebViewUserDataFolder);
        }

        public void SetupWebViewEnvironment()
        {
            if (_webView == null) return;
            _webView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = _userDataFolder
            };
        }

        public async Task InitializeAsync()
        {
            if (_webView == null) return;
            await _webView.EnsureCoreWebView2Async();
            _webView.SourceChanged += WebView_SourceChanged;
        }

        private async void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            string currentUrl = _webView.Source.ToString().ToLower();
            if (currentUrl.Contains("bbs") && !currentUrl.Contains("seafile.bbs-me.org"))
            {
                await _webView.ExecuteScriptAsync(AppConstants.Scripts.SeafileAutoClicker);
            }
        }

        public void LoadLoginPage()
        {
            _webView.Source = new Uri(AppConfig.LoginUrl);
        }

        public void ClearBrowserCacheOnDisk()
        {
            try
            {
                if (Directory.Exists(_userDataFolder))
                {
                    Directory.Delete(_userDataFolder, true);
                }
            }
            catch (Exception) { }
        }

        public async Task<string> TryExtractTokenAsync()
        {
            if (_webView?.CoreWebView2 == null) return null;
            try
            {
                // Hole alle Cookies
                var result = await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.getAllCookies", "{}");
                JObject jsonResult = JObject.Parse(result);
                JArray cookies = (JArray)jsonResult["cookies"];

                string foundAuthToken = null;
                List<string> cookieParts = new List<string>();

                foreach (var cookie in cookies)
                {
                    string name = cookie["name"]?.ToString();
                    string value = cookie["value"]?.ToString();

                    // Cookie für den globalen "Browser-Header" sammeln
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    {
                        cookieParts.Add($"{name}={value}");
                    }

                    // 1. Auth Token suchen (Extract aus seahub_auth wenn möglich)
                    if (name == "seahub_auth")
                    {
                        Match match = Regex.Match(value, @"[0-9a-f]{40}");
                        if (match.Success) foundAuthToken = match.Value;
                    }

                    // 2. CSRF Token suchen und global speichern
                    if (name == "sfcsrftoken" || name == "csrftoken")
                    {
                        AppConfig.CSRFToken = value;
                    }
                }

                // Speichere ALLE Cookies als String (Wichtig für Move/Post Operations)
                AppConfig.RawCookies = string.Join("; ", cookieParts);

                return foundAuthToken;
            }
            catch { }
            return null;
        }
    }
}