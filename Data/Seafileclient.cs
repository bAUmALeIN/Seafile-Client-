using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class SeafileClient
    {
        private readonly HttpClient _httpClient;

        public SeafileClient(string token)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + token);
            // Genereller Timeout für API Anfragen
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // ========================================================================
        // STANDARD API METHODEN
        // ========================================================================

        public async Task<List<SeafileRepo>> GetLibrariesAsync()
        {
            string url = AppConfig.ApiBaseUrl + "repos/";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<SeafileRepo>>(json);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token ist abgelaufen oder ungültig.");
            }
            else
            {
                throw new Exception($"API Fehler: {response.StatusCode}");
            }
        }

        public async Task<List<SeafileEntry>> GetDirectoryEntriesAsync(string repoId, string path = "/")
        {
            string encodedPath = System.Net.WebUtility.UrlEncode(path);
            string url = $"{AppConfig.ApiBaseUrl}repos/{repoId}/dir/?p={encodedPath}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<SeafileEntry>>(json);
            }
            else
            {
                throw new Exception($"Fehler beim Laden des Ordners: {response.StatusCode}");
            }
        }

        public async Task<bool> CreateLibraryAsync(string name)
        {
            string url = AppConfig.ApiBaseUrl + "repos/";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("name", name)
            };
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateDirectoryAsync(string repoId, string path)
        {
            string encodedPath = System.Net.WebUtility.UrlEncode(path);
            string url = $"{AppConfig.ApiBaseUrl}repos/{repoId}/dir/?p={encodedPath}";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("operation", "mkdir")
            };
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEntryAsync(string repoId, string path, bool isDir)
        {
            string encodedPath = System.Net.WebUtility.UrlEncode(path);
            string type = isDir ? "dir" : "file";
            string url = $"{AppConfig.ApiBaseUrl}repos/{repoId}/{type}/?p={encodedPath}";
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteLibraryAsync(string repoId)
        {
            string url = $"{AppConfig.ApiBaseUrl}repos/{repoId}/";
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

        // ========================================================================
        // UPLOAD LOGIK (MANUELL & ROBUST)
        // ========================================================================

        public async Task<string> GetUploadLinkAsync(string repoId, string path)
        {
            string encodedPath = System.Net.WebUtility.UrlEncode(path);
            string url = $"{AppConfig.ApiBaseUrl}repos/{repoId}/upload-link/?p={encodedPath}";

            var response = await _httpClient.GetAsync(url);
            string rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                //MessageBox.Show($"Fehler beim Link holen:\nStatus: {response.StatusCode}\nAntwort: {rawResponse}");
                return null;
            }

            // Bereinigen (Anführungszeichen etc. weg)
            string cleanResponse = rawResponse.Trim('"', ' ', '\r', '\n');

            // Prüfen ob JSON zurückkam
            if (cleanResponse.StartsWith("{"))
            {
                try
                {
                    JObject json = JObject.Parse(cleanResponse);
                    // Verschiedene Keys prüfen, die Seafile nutzen könnte
                    if (json["url"] != null) return json["url"].ToString();
                    if (json["upload_link"] != null) return json["upload_link"].ToString();
                    if (json["link"] != null) return json["link"].ToString();
                }
                catch
                {
                    // Fallback: Wenn JSON Parse fehlschlägt, nehmen wir den String so wie er ist
                }
            }

            return cleanResponse;
        }

        public async Task<bool> UploadFileAsync(string uploadLink, string filePath, string targetFolder, string fileName)
        {
            // 1. Validierung
            if (!File.Exists(filePath)) throw new FileNotFoundException("Datei nicht gefunden", filePath);
            if (new FileInfo(filePath).Length == 0) throw new Exception("Datei ist leer (0 Byte).");

            // 2. Link vorbereiten (JSON Antwort erzwingen)
            uploadLink = uploadLink.Trim();
            if (!uploadLink.Contains("ret-json=1"))
            {
                uploadLink += (uploadLink.Contains("?") ? "&" : "?") + "ret-json=1";
            }

            // 3. Manueller Body-Bau (Umgeht .NET Formatierungsprobleme)
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

            // Wir nutzen eine Liste von Bytes, um alles zusammenzubauen
            List<byte> bodyBytes = new List<byte>();

            // Hilfsfunktion zum Hinzufügen von Strings als UTF8 Bytes
            void AddString(string s) => bodyBytes.AddRange(Encoding.UTF8.GetBytes(s));

            // -- TEIL A: parent_dir --
            AddString("--" + boundary + "\r\n");
            AddString("Content-Disposition: form-data; name=\"parent_dir\"\r\n\r\n");
            AddString(targetFolder);
            AddString("\r\n");

            // -- TEIL B: replace (1 = überschreiben) --
            AddString("--" + boundary + "\r\n");
            AddString("Content-Disposition: form-data; name=\"replace\"\r\n\r\n");
            AddString("1");
            AddString("\r\n");

            // -- TEIL C: Datei Header --
            AddString("--" + boundary + "\r\n");
            // Wichtig: Filename in Anführungszeichen
            AddString($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n");
            AddString("Content-Type: application/octet-stream\r\n\r\n");

            // -- TEIL D: Datei Inhalt --
            byte[] fileBytes = File.ReadAllBytes(filePath);
            bodyBytes.AddRange(fileBytes);
            AddString("\r\n");

            // -- TEIL E: Footer --
            AddString("--" + boundary + "--\r\n");


            // 4. Senden
            using (var uploader = new HttpClient())
            {
                // Timeout hochsetzen für große Dateien
                uploader.Timeout = TimeSpan.FromMinutes(10);
                uploader.DefaultRequestHeaders.UserAgent.ParseAdd("SeafileWinFormsClient/1.0");
                // "Expect: 100-continue" deaktivieren (WICHTIG!)
                uploader.DefaultRequestHeaders.ExpectContinue = false;

                // ByteArrayContent erstellen aus unserem handgebauten Body
                var byteContent = new ByteArrayContent(bodyBytes.ToArray());

                // Content-Type Header manuell setzen, damit Boundary stimmt
                byteContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

                try
                {
                    var response = await uploader.PostAsync(uploadLink, byteContent);
                    string serverResponse = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Nur bei Fehler anzeigen wir die Debug-Info
                        MessageBox.Show($"UPLOAD FEHLER!\nCode: {response.StatusCode}\nMsg: {serverResponse}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Netzwerk-Fehler: {ex.Message}");
                    throw;
                }
            }
        }
    }
}