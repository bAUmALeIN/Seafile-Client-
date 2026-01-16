using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using System;
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
            _webView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = _userDataFolder
            };
        }

        public async Task InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async();


            _webView.SourceChanged += WebView_SourceChanged;
        }


        private async void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            string currentUrl = _webView.Source.ToString().ToLower();


            if (currentUrl.Contains("bbs") && !currentUrl.Contains("seafile.bbs-me.org"))
            {
                string script = @"
                    (function() {
                        //(Lock)
                        if (window.seafileAutoClickerRunning) return;
                        window.seafileAutoClickerRunning = true;

                        console.log('Auto-Clicker gestartet für: ' + window.location.href);

                        var attempts = 0;
                        var maxAttempts = 60; // 30 Sekunden lang probieren
                        
                        function tryClickSeafile() {
                            attempts++;
                            
                            // Conatiner Suche 
                            var apps = document.querySelectorAll('.appBubbleClass');
                            
                            
                            if (apps.length === 0) return false;

                            for(var i = 0; i < apps.length; i++) {
                                
                                var text = (apps[i].innerText || '').toLowerCase();
                                
                                if(text.includes('seafile')) {
                                    console.log('Seafile gefunden! Klicke...');

                                    
                                    var btn = apps[i].querySelector('button');
                                    if(btn) {
                                        btn.click();
                                        return true; 
                                    }
                                    
                                    
                                    apps[i].click(); 
                                    return true;
                                }
                            }
                            return false;
                        }

                        
                        var intervalId = setInterval(function() {
                            var success = tryClickSeafile();
                            
                            
                            if (success || attempts >= maxAttempts) {
                                clearInterval(intervalId);
                                // Lock wieder freigeben (optional, eigentlich sind wir dann eh weg)
                                window.seafileAutoClickerRunning = false; 
                            }
                        }, 500);
                    })();
                ";

                await _webView.ExecuteScriptAsync(script);
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
            if (_webView.CoreWebView2 == null) return null;

            try
            {
                var result = await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.getAllCookies", "{}");
                JObject jsonResult = JObject.Parse(result);
                JArray cookies = (JArray)jsonResult["cookies"];

                foreach (var cookie in cookies)
                {
                    string name = cookie["name"]?.ToString();
                    string value = cookie["value"]?.ToString();

                    if (name == "seahub_auth")
                    {
                        Match match = Regex.Match(value, @"[0-9a-f]{40}");
                        if (match.Success) return match.Value;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}