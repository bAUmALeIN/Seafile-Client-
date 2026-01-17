using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class SeafileClient
    {
        private readonly HttpClient _apiClient; // Für API Befehle (mit Token)
        private readonly HttpClient _fileDownloader; // Für Datei-Downloads (OHNE Token Header!)
        private readonly string _token;

        public SeafileClient(string token)
        {
            _token = token;

            // 1. API Client (mit Auth Header)
            _apiClient = new HttpClient();
            _apiClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            _apiClient.DefaultRequestHeaders.Add("Authorization", "Token " + token);
            _apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) SeafileExplorer/1.0");
            _apiClient.Timeout = TimeSpan.FromMinutes(5);

            // 2. File Downloader (SAUBER, keine Header!)
            _fileDownloader = new HttpClient();
            _fileDownloader.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) SeafileExplorer/1.0");
            _fileDownloader.Timeout = TimeSpan.FromMinutes(60); // Lange Timeouts für große Downloads
        }

        // ==========================================================
        // BIBLIOTHEKEN & DATEIEN
        // ==========================================================

        public async Task<List<SeafileRepo>> GetLibrariesAsync()
        {
            string json = await GetStringAsync("repos/");
            return JsonConvert.DeserializeObject<List<SeafileRepo>>(json) ?? new List<SeafileRepo>();
        }

        public async Task<List<SeafileEntry>> GetDirectoryEntriesAsync(string repoId, string path)
        {
            string encodedPath = Uri.EscapeDataString(path);
            string url = $"repos/{repoId}/dir/?p={encodedPath}";

            string json = await GetStringAsync(url);
            try
            {
                return JsonConvert.DeserializeObject<List<SeafileEntry>>(json);
            }
            catch
            {
                return new List<SeafileEntry>();
            }
        }

        public async Task<List<SeafileEntry>> GetAllFilesRecursiveAsync(string repoId, string path = "/")
        {
            string encodedPath = Uri.EscapeDataString(path);
            string url = $"repos/{repoId}/dir/?p={encodedPath}&recursive=1";

            string json = await GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<SeafileEntry>>(json) ?? new List<SeafileEntry>();
        }

        // ==========================================================
        // OPERATIONEN
        // ==========================================================

        public async Task<bool> CreateLibraryAsync(string name)
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("name", name) });
            var response = await _apiClient.PostAsync("repos/", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateDirectoryAsync(string repoId, string path)
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("operation", "mkdir") });
            string encodedPath = Uri.EscapeDataString(path);
            var response = await _apiClient.PostAsync($"repos/{repoId}/dir/?p={encodedPath}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteLibraryAsync(string repoId)
        {
            var response = await _apiClient.DeleteAsync($"repos/{repoId}/");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEntryAsync(string repoId, string path, bool isDir)
        {
            string encodedPath = Uri.EscapeDataString(path);
            string url = isDir ? $"repos/{repoId}/dir/?p={encodedPath}" : $"repos/{repoId}/file/?p={encodedPath}";
            var response = await _apiClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

        // ==========================================================
        // DOWNLOAD LINK GENERATOREN
        // ==========================================================

        public async Task<string> GetDownloadLinkAsync(string repoId, string path)
        {
            string encodedPath = Uri.EscapeDataString(path);
            string url = $"repos/{repoId}/file/?p={encodedPath}&reuse=1";
            string json = await GetStringAsync(url);
            return json.Trim('"');
        }

        public async Task<string> GetDirectoryZipLinkAsync(string repoId, string fullPath)
        {
            string cleanPath = fullPath.Trim('/');

            // Root Download
            if (string.IsNullOrEmpty(cleanPath))
            {
                string url = $"repos/{repoId}/download/";
                string json = await GetStringAsync(url);
                return json.Trim('"');
            }

            string parentDir = "/";
            string dirName = cleanPath;
            int lastSlash = cleanPath.LastIndexOf('/');
            if (lastSlash > -1)
            {
                parentDir = "/" + cleanPath.Substring(0, lastSlash);
                dirName = cleanPath.Substring(lastSlash + 1);
            }

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("parent_dir", parentDir),
                new KeyValuePair<string, string>("dirents", dirName)
            });

            // 1. Versuch: API v2.0
            var response = await _apiClient.PostAsync($"repos/{repoId}/zip-task/", content);

            // 2. Versuch: API v2.1
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                string v21Url = AppConfig.ApiBaseUrl.Replace("/api2/", "/api/v2.1/") + $"repos/{repoId}/zip-task/";
                response = await _apiClient.PostAsync(v21Url, content);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("SERVER_NO_ZIP");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(jsonResponse);
            string zipToken = obj["zip_token"]?.ToString();

            if (string.IsNullOrEmpty(zipToken)) throw new Exception("SERVER_NO_ZIP");

            string rootUrl = new Uri(AppConfig.ApiBaseUrl).GetLeftPart(UriPartial.Authority);
            return $"{rootUrl}/seafhttp/zip/{zipToken}";
        }

        public async Task<string> GetUploadLinkAsync(string repoId, string path)
        {
            string encodedPath = Uri.EscapeDataString(path);
            string url = $"repos/{repoId}/upload-link/?p={encodedPath}";
            string json = await GetStringAsync(url);
            return json.Trim('"');
        }

        // ==========================================================
        // UPLOAD & STREAMING (WICHTIG: Nutzt _fileDownloader!)
        // ==========================================================

        public async Task UploadFileAsync(string uploadLink, string localFilePath, string targetFolder, string fileName)
        {
            using (var multipartFormContent = new MultipartFormDataContent())
            using (var fileStream = File.OpenRead(localFilePath))
            {
                multipartFormContent.Add(new StreamContent(fileStream), "file", fileName);
                multipartFormContent.Add(new StringContent(targetFolder), "parent_dir");

                // Upload nutzt auch besser den sauberen Client, sicherheitshalber
                var response = await _fileDownloader.PostAsync(uploadLink, multipartFormContent);
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Upload fehlgeschlagen: {response.ReasonPhrase}");
            }
        }

        public async Task DownloadFileAsStreamAsync(string url, string localOutputPath)
        {
            // HIER WAR DER FEHLER: Wir nutzen jetzt _fileDownloader statt _apiClient!
            using (var response = await _fileDownloader.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Download-Status: {response.StatusCode}");

                if (response.Content.Headers.ContentType != null &&
                    response.Content.Headers.ContentType.MediaType.Contains("text/html"))
                {
                    throw new Exception("HTML_ERROR");
                }

                using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                using (var streamToWriteTo = File.Open(localOutputPath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                }
            }
        }

        private async Task<string> GetStringAsync(string endpoint)
        {
            var response = await _apiClient.GetAsync(endpoint);
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new Exception("Sitzung abgelaufen.");
            if (!response.IsSuccessStatusCode) throw new Exception($"API Fehler ({response.StatusCode})");
            return await response.Content.ReadAsStringAsync();
        }
    }
}