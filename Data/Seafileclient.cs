using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace WinFormsApp3.Data
{
    public class SeafileClient
    {
        private static readonly HttpClient _apiClient;
        private static readonly HttpClient _fileDownloader;
        private readonly string _token;

        static SeafileClient()
        {
            _apiClient = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl), Timeout = TimeSpan.FromMinutes(5) };
            _fileDownloader = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        }

        public SeafileClient(string token)
        {
            _token = token;
            if (!_apiClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _apiClient.DefaultRequestHeaders.Add("Authorization", "Token " + token);
                _apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("SeafileExplorer/1.0");
            }
            if (!_fileDownloader.DefaultRequestHeaders.UserAgent.Any())
            {
                _fileDownloader.DefaultRequestHeaders.UserAgent.ParseAdd("SeafileExplorer/1.0");
            }
        }

        public async Task<List<SeafileRepo>> GetLibrariesAsync()
        {
            string json = await GetStringAsync("repos/");
            return JsonConvert.DeserializeObject<List<SeafileRepo>>(json) ?? new List<SeafileRepo>();
        }

        public async Task<List<SeafileEntry>> GetDirectoryEntriesAsync(string repoId, string path)
        {
            string json = await GetStringAsync($"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}");
            try { return JsonConvert.DeserializeObject<List<SeafileEntry>>(json); } catch { return new List<SeafileEntry>(); }
        }

        public async Task<List<SeafileEntry>> GetAllFilesRecursiveAsync(string repoId, string path = "/")
        {
            string json = await GetStringAsync($"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}&recursive=1");
            return JsonConvert.DeserializeObject<List<SeafileEntry>>(json) ?? new List<SeafileEntry>();
        }

        public async Task<bool> CreateLibraryAsync(string name)
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("name", name) });
            return (await _apiClient.PostAsync("repos/", content)).IsSuccessStatusCode;
        }

        public async Task<bool> CreateDirectoryAsync(string repoId, string path)
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("operation", "mkdir") });
            return (await _apiClient.PostAsync($"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}", content)).IsSuccessStatusCode;
        }

        public async Task<bool> DeleteLibraryAsync(string repoId) => (await _apiClient.DeleteAsync($"repos/{repoId}/")).IsSuccessStatusCode;

        public async Task<bool> DeleteEntryAsync(string repoId, string path, bool isDir)
        {
            string url = isDir ? $"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}" : $"repos/{repoId}/file/?p={Uri.EscapeDataString(path)}";
            return (await _apiClient.DeleteAsync(url)).IsSuccessStatusCode;
        }

        public async Task<string> GetDownloadLinkAsync(string repoId, string path)
            => SafeExtractString(await GetStringAsync($"repos/{repoId}/file/?p={Uri.EscapeDataString(path)}&reuse=1"));

        public async Task<string> GetUploadLinkAsync(string repoId, string path)
        {
            string json = await GetStringAsync($"repos/{repoId}/upload-link/?p={Uri.EscapeDataString(path)}");
            return SafeExtractString(json);
        }

        public async Task<string> GetDirectoryZipLinkAsync(string repoId, string fullPath)
        {
            string cleanPath = fullPath.Trim('/');
            var content = string.IsNullOrEmpty(cleanPath) ? null : new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("parent_dir", "/"), new KeyValuePair<string, string>("dirents", cleanPath) });
            var response = await _apiClient.PostAsync($"repos/{repoId}/zip-task/", content);
            if (!response.IsSuccessStatusCode) throw new Exception("SERVER_NO_ZIP");
            JObject obj = JObject.Parse(await response.Content.ReadAsStringAsync());
            return $"{new Uri(AppConfig.ApiBaseUrl).GetLeftPart(UriPartial.Authority)}/seafhttp/zip/{obj["zip_token"]}";
        }

        private string SafeExtractString(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                if (json.Trim().StartsWith("\"")) return json.Trim('"');
                var token = JToken.Parse(json);
                return token.Type == JTokenType.String ? token.ToString() : json.Trim('"');
            }
            catch { return json.Trim('"'); }
        }

        // =================================================================================
        // MANUELLE UPLOAD IMPLEMENTIERUNG (Behebt 400 Bad Request zuverlässig)
        // =================================================================================
        public async Task UploadFileWithProgressAsync(string uploadLink, string localFilePath, string targetPath, string fileName, Action<int> onProgress)
        {
            // Pfad-Bereinigung
            string parentDir = targetPath.Replace("\\", "/").Trim();
            if (!parentDir.StartsWith("/")) parentDir = "/" + parentDir;
            if (parentDir.Length > 1 && parentDir.EndsWith("/")) parentDir = parentDir.TrimEnd('/');

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // Wir bauen den Request Body manuell zusammen, um die volle Kontrolle über Header zu haben.
            using (var fileStream = File.OpenRead(localFilePath))
            using (var contentStream = new MemoryStream())
            {
                // 1. Parameter: parent_dir
                WriteMultipartParam(contentStream, boundary, "parent_dir", parentDir);

                // 2. Parameter: replace
                WriteMultipartParam(contentStream, boundary, "replace", "1");

                // 3. File Header
                string fileHeader = $"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                byte[] fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
                contentStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);

                // Request starten
                var request = new HttpRequestMessage(HttpMethod.Post, uploadLink);
                request.Headers.Add("User-Agent", "SeafileExplorer/1.0"); // Seafile mag User-Agents

                // WICHTIG: Den Stream kombinieren (Header + Datei + Footer)
                // Da MemoryStream für große Dateien schlecht ist, nutzen wir eine Custom Content Klasse
                var combinedContent = new ManualMultipartContent(contentStream.ToArray(), fileStream, endBoundaryBytes, boundary, (sent, total) =>
                {
                    onProgress?.Invoke((int)((double)sent / total * 100));
                });

                request.Content = combinedContent;

                var response = await _fileDownloader.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Upload fehlgeschlagen (HTTP {response.StatusCode}): {errorMsg}");
                }
            }
        }

        private void WriteMultipartParam(Stream stream, string boundary, string name, string value)
        {
            string header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(header);
            stream.Write(bytes, 0, bytes.Length);
        }

        public async Task DownloadFileWithProgressAsync(string url, string localOutputPath, Action<int> onProgress)
        {
            using (var resp = await _fileDownloader.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!resp.IsSuccessStatusCode) throw new Exception($"Status: {resp.StatusCode}");
                long? total = resp.Content.Headers.ContentLength;
                using (var sRead = await resp.Content.ReadAsStreamAsync())
                using (var sWrite = File.Open(localOutputPath, FileMode.Create))
                {
                    byte[] buffer = new byte[8192];
                    long readTotal = 0;
                    int read;
                    while ((read = await sRead.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await sWrite.WriteAsync(buffer, 0, read);
                        readTotal += read;
                        if (total.HasValue) onProgress?.Invoke((int)((double)readTotal / total.Value * 100));
                    }
                }
            }
        }

        private async Task<string> GetStringAsync(string endpoint)
        {
            var resp = await _apiClient.GetAsync(endpoint);
            string content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"API Error ({resp.StatusCode}): {content}");
            return content;
        }
    }

    // Hilfsklasse für manuellen Multipart-Upload ohne Speicherüberlauf
    public class ManualMultipartContent : HttpContent
    {
        private readonly byte[] _head;
        private readonly Stream _fileStream;
        private readonly byte[] _tail;
        private readonly Action<long, long> _progress;
        private readonly long _totalSize;

        public ManualMultipartContent(byte[] head, Stream fileStream, byte[] tail, string boundary, Action<long, long> progress)
        {
            _head = head;
            _fileStream = fileStream;
            _tail = tail;
            _progress = progress;
            _totalSize = _head.Length + _fileStream.Length + _tail.Length;

            // Wichtig: Content-Type Header manuell setzen, aber ohne Anführungszeichen bei Boundary!
            Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _totalSize;
            return true;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            // 1. Header schreiben (Parameter)
            await stream.WriteAsync(_head, 0, _head.Length);

            // 2. Datei streamen mit Progress
            byte[] buffer = new byte[8192];
            long bytesSent = _head.Length;
            int bytesRead;

            if (_fileStream.CanSeek) _fileStream.Position = 0;

            while ((bytesRead = await _fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
                bytesSent += bytesRead;
                _progress?.Invoke(bytesSent, _totalSize);
            }

            // 3. Ende schreiben
            await stream.WriteAsync(_tail, 0, _tail.Length);
            _progress?.Invoke(_totalSize, _totalSize);
        }
    }
}