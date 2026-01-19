using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class SeafileClient
    {
        private HttpClient _apiClient;
        private HttpClient _fileDownloader;
        private readonly string _token;

        public SeafileClient(string token)
        {
            _token = token;
            InitializeClients();
        }

        private void InitializeClients()
        {
            // SICHERHEIT: Wir deaktivieren das automatische Cookie-Handling.
            // Wir injizieren die Cookies aus dem WebView2 manuell. Das verhindert Konflikte.
            var handler = new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false };

            _apiClient = new HttpClient(handler) { BaseAddress = new Uri(AppConfig.ApiBaseUrl), Timeout = TimeSpan.FromMinutes(5) };

            // Downloader braucht Redirects, aber keine Auto-Cookies
            var downloadHandler = new HttpClientHandler { UseCookies = false, AllowAutoRedirect = true };
            _fileDownloader = new HttpClient(downloadHandler) { Timeout = TimeSpan.FromMinutes(60) };

            // Default Headers setzen
            ApplyDefaultHeaders(_apiClient);
            ApplyDefaultHeaders(_fileDownloader);
        }

        private void ApplyDefaultHeaders(HttpClient client)
        {
            if (!string.IsNullOrEmpty(_token))
            {
                // Manche Endpoints wollen "Token xxx", andere nur Session. Wir senden beides wenn möglich.
                if (!client.DefaultRequestHeaders.Contains("Authorization"))
                    client.DefaultRequestHeaders.Add("Authorization", "Token " + _token);
            }

            // User-Agent ist wichtig, damit wir wie ein Browser aussehen
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        // Hilfsmethode, um Requests vor dem Senden "Browser-tauglich" zu machen
        private void InjectBrowserHeaders(HttpRequestMessage request)
        {
            request.Headers.Referrer = new Uri(AppConfig.ApiBaseUrl);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            if (!string.IsNullOrEmpty(AppConfig.CSRFToken))
            {
                if (request.Headers.Contains("X-CSRFToken")) request.Headers.Remove("X-CSRFToken");
                request.Headers.Add("X-CSRFToken", AppConfig.CSRFToken);
            }

            if (!string.IsNullOrEmpty(AppConfig.RawCookies))
            {
                if (request.Headers.Contains("Cookie")) request.Headers.Remove("Cookie");
                request.Headers.Add("Cookie", AppConfig.RawCookies);
            }
        }

        // --- CORE OPERATIONS (SYNC-BATCH STRATEGY - JSON) ---

        public async Task<bool> MoveEntryAsync(string repoId, string srcPath, string dstDir, bool isDir, string dstRepoId = null)
        {
            return await ExecuteBatchOperationAsync("move", repoId, srcPath, dstDir, dstRepoId);
        }

        public async Task<bool> CopyEntryAsync(string repoId, string srcPath, string dstDir, bool isDir, string dstRepoId = null)
        {
            return await ExecuteBatchOperationAsync("copy", repoId, srcPath, dstDir, dstRepoId);
        }

        private async Task<bool> ExecuteBatchOperationAsync(string operation, string repoId, string srcPath, string dstDir, string dstRepoId)
        {
            if (string.IsNullOrEmpty(dstRepoId)) dstRepoId = repoId;

            // 1. Pfade bereinigen
            srcPath = srcPath.Replace("\\", "/").TrimEnd('/');
            if (dstDir.Length > 1 && dstDir.EndsWith("/")) dstDir = dstDir.TrimEnd('/');
            if (string.IsNullOrEmpty(dstDir)) dstDir = "/";

            // 2. Zerlegung in Parent-Dir und Dateiname
            int lastSlash = srcPath.LastIndexOf('/');
            string parentDir = lastSlash <= 0 ? "/" : srcPath.Substring(0, lastSlash);
            string objName = srcPath.Substring(lastSlash + 1);

            // 3. Payload bauen (JSON Format laut deinen Screenshots)
            // Endpoint: /api/v2.1/repos/sync-batch-move-item/
            // Body: JSON mit src_dirents als Array
            var payloadObj = new
            {
                src_repo_id = repoId,
                src_parent_dir = parentDir,
                dst_repo_id = dstRepoId,
                dst_parent_dir = dstDir,
                src_dirents = new[] { objName } // WICHTIG: Das Array aus dem Screenshot!
            };

            string jsonBody = JsonConvert.SerializeObject(payloadObj);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 4. URL Bauen - Wir nutzen jetzt den "Sync-Batch" Endpoint
            string baseUrl = AppConfig.ApiBaseUrl.TrimEnd('/');
            if (baseUrl.EndsWith("api2")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 4);
            baseUrl = baseUrl.TrimEnd('/');

            string endpointOp = operation == "move" ? "sync-batch-move-item" : "sync-batch-copy-item";
            string url = $"{baseUrl}/api/v2.1/repos/{endpointOp}/";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            request.Headers.Authorization = null; // Nur Session Auth
            InjectBrowserHeaders(request);

            var response = await _apiClient.SendAsync(request);

            if (response.IsSuccessStatusCode) return true;

            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"SyncBatchOp Failed ({response.StatusCode}): {error}");
        }

        // --- STANDARD METHODEN ---

        public void ReloadSettings() { _apiClient?.Dispose(); _fileDownloader?.Dispose(); InitializeClients(); }

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
            var req = new HttpRequestMessage(HttpMethod.Post, "repos/") { Content = content };
            InjectBrowserHeaders(req);
            return (await _apiClient.SendAsync(req)).IsSuccessStatusCode;
        }

        public async Task<bool> CreateDirectoryAsync(string repoId, string path)
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("operation", "mkdir") });
            var req = new HttpRequestMessage(HttpMethod.Post, $"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}") { Content = content };
            InjectBrowserHeaders(req);
            return (await _apiClient.SendAsync(req)).IsSuccessStatusCode;
        }

        public async Task<bool> DeleteLibraryAsync(string repoId)
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, $"repos/{repoId}/");
            InjectBrowserHeaders(req);
            return (await _apiClient.SendAsync(req)).IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEntryAsync(string repoId, string path, bool isDir)
        {
            string url = isDir ? $"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}" : $"repos/{repoId}/file/?p={Uri.EscapeDataString(path)}";
            var req = new HttpRequestMessage(HttpMethod.Delete, url);
            InjectBrowserHeaders(req);
            return (await _apiClient.SendAsync(req)).IsSuccessStatusCode;
        }

        public async Task<Image> GetThumbnailAsync(string repoId, string path, int size)
        {
            try
            {
                string url = $"repos/{repoId}/thumbnail/?p={Uri.EscapeDataString(path)}&size={size}";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                InjectBrowserHeaders(req);
                var resp = await _apiClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    using (var ms = new MemoryStream(await resp.Content.ReadAsByteArrayAsync())) return Image.FromStream(ms);
                }
            }
            catch { }
            return null;
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
            var req = new HttpRequestMessage(HttpMethod.Post, $"repos/{repoId}/zip-task/") { Content = content };
            InjectBrowserHeaders(req);
            var response = await _apiClient.SendAsync(req);

            if (!response.IsSuccessStatusCode) throw new Exception("SERVER_NO_ZIP");
            JObject obj = JObject.Parse(await response.Content.ReadAsStringAsync());
            return $"{new Uri(AppConfig.ApiBaseUrl).GetLeftPart(UriPartial.Authority)}/seafhttp/zip/{obj["zip_token"]}";
        }

        // --- DOWNLOAD & UPLOAD HELPERS ---

        public async Task DownloadFileWithProgressAsync(string url, string localOutputPath, Action<long, long> onProgress)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            InjectBrowserHeaders(request);

            using (var resp = await _fileDownloader.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!resp.IsSuccessStatusCode) throw new Exception($"Status: {resp.StatusCode}");
                long total = resp.Content.Headers.ContentLength ?? 0;

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
                        onProgress?.Invoke(readTotal, total);
                    }
                }
            }
        }

        public async Task UploadFileWithProgressAsync(string uploadLink, string localFilePath, string targetPath, string fileName, Action<long, long> onProgress)
        {
            string parentDir = targetPath.Replace("\\", "/").Trim();
            if (!parentDir.StartsWith("/")) parentDir = "/" + parentDir;
            if (parentDir.Length > 1 && parentDir.EndsWith("/")) parentDir = parentDir.TrimEnd('/');

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            using (var fileStream = File.OpenRead(localFilePath))
            using (var contentStream = new MemoryStream())
            {
                WriteMultipartParam(contentStream, boundary, "parent_dir", parentDir);
                WriteMultipartParam(contentStream, boundary, "replace", "0");

                string fileHeader = $"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                byte[] fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
                contentStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);

                var request = new HttpRequestMessage(HttpMethod.Post, uploadLink);
                InjectBrowserHeaders(request);

                var combinedContent = new ManualMultipartContent(contentStream.ToArray(), fileStream, endBoundaryBytes, boundary, onProgress);
                request.Content = combinedContent;

                var response = await _fileDownloader.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    string rawError = await response.Content.ReadAsStringAsync();
                    throw new Exception(ParseSeafileError(response.StatusCode, rawError));
                }
            }
        }

        // --- INTERNALS ---

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

        private string ParseSeafileError(HttpStatusCode code, string rawBody)
        {
            string clean = rawBody.Trim();
            if (code == HttpStatusCode.BadRequest)
            {
                if (clean.Contains("parent_dir")) return "Zielordner existiert nicht oder Pfad ungültig.";
                if (clean.Contains("file already exists")) return "Datei existiert bereits.";
                if (clean.Contains("quota")) return "Speicherplatz voll.";
            }
            if (code == HttpStatusCode.RequestEntityTooLarge) return "Datei ist zu groß.";
            if (code == HttpStatusCode.Forbidden) return "Keine Schreibrechte.";
            return $"Server Fehler ({(int)code}): {clean}";
        }

        private void WriteMultipartParam(Stream stream, string boundary, string name, string value)
        {
            string header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(header);
            stream.Write(bytes, 0, bytes.Length);
        }

        private async Task<string> GetStringAsync(string endpoint)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
            InjectBrowserHeaders(req);
            var resp = await _apiClient.SendAsync(req);
            string content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"API Error ({resp.StatusCode}): {content}");
            return content;
        }
    }

    // Hilfsklasse für Upload (zwingend erforderlich für UploadFileWithProgressAsync)
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
            Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _totalSize;
            return true;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(_head, 0, _head.Length);
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
            await stream.WriteAsync(_tail, 0, _tail.Length);
            _progress?.Invoke(_totalSize, _totalSize);
        }
    }
}