using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers; // Wichtig für AuthenticationHeaderValue
using System.Text;
using System.Threading.Tasks;

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
            _apiClient = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl), Timeout = TimeSpan.FromMinutes(5) };
            _fileDownloader = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };

            if (!string.IsNullOrEmpty(_token))
            {
                _apiClient.DefaultRequestHeaders.Add("Authorization", "Token " + _token);
            }

            // Standard Header für CSRF Schutz
            if (!string.IsNullOrEmpty(AppConfig.CSRFToken))
            {
                _apiClient.DefaultRequestHeaders.Add("X-CSRFToken", AppConfig.CSRFToken);
            }
            _apiClient.DefaultRequestHeaders.Add("Referer", AppConfig.ApiBaseUrl); // Seafile prüft oft Referer

            _apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("SeafileExplorer/1.0");
            _fileDownloader.DefaultRequestHeaders.UserAgent.ParseAdd("SeafileExplorer/1.0");
        }

        public void ReloadSettings()
        {
            _apiClient?.Dispose();
            _fileDownloader?.Dispose();
            InitializeClients();
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
            string url = isDir ?
                $"repos/{repoId}/dir/?p={Uri.EscapeDataString(path)}" : $"repos/{repoId}/file/?p={Uri.EscapeDataString(path)}";
            return (await _apiClient.DeleteAsync(url)).IsSuccessStatusCode;
        }

        // --- MOVE OPERATION (HARDENED) ---
        public async Task<bool> MoveEntryAsync(string repoId, string srcPath, string dstDir, string dstRepoId = null)
        {
            if (string.IsNullOrEmpty(dstRepoId)) dstRepoId = repoId;

            // Pfad-Bereinigung für Seafile
            if (dstDir.Length > 1 && dstDir.EndsWith("/")) dstDir = dstDir.TrimEnd('/');
            if (string.IsNullOrEmpty(dstDir)) dstDir = "/";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("operation", "move"),
                new KeyValuePair<string, string>("dst_repo", dstRepoId),
                new KeyValuePair<string, string>("dst_dir", dstDir)
            });

            string url = $"repos/{repoId}/file/?p={Uri.EscapeDataString(srcPath)}";

            // Manuell Request bauen, um Header zu erzwingen
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;

            // Authorization explizit setzen (überschreibt ggf. Fehler im DefaultHeader)
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

            // CSRF Token falls vorhanden (ESSENTIELL für Session Auth)
            if (!string.IsNullOrEmpty(AppConfig.CSRFToken))
            {
                if (request.Headers.Contains("X-CSRFToken")) request.Headers.Remove("X-CSRFToken");
                request.Headers.Add("X-CSRFToken", AppConfig.CSRFToken);
            }

            // Referer setzen (gegen strikte Django Checks)
            request.Headers.Referrer = new Uri(AppConfig.ApiBaseUrl);

            var response = await _apiClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Server Meldung ({response.StatusCode}): {errorBody}");
            }
            return true;
        }

        public async Task<Image> GetThumbnailAsync(string repoId, string path, int size)
        {
            try
            {
                string url = $"repos/{repoId}/thumbnail/?p={Uri.EscapeDataString(path)}&size={size}";
                var bytes = await _apiClient.GetByteArrayAsync(url);
                if (bytes != null && bytes.Length > 0)
                {
                    using (var ms = new MemoryStream(bytes)) return Image.FromStream(ms);
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
                request.Headers.Add("User-Agent", "SeafileExplorer/1.0");

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

        public async Task DownloadFileWithProgressAsync(string url, string localOutputPath, Action<long, long> onProgress)
        {
            using (var resp = await _fileDownloader.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
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

        private async Task<string> GetStringAsync(string endpoint)
        {
            var resp = await _apiClient.GetAsync(endpoint);
            string content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"API Error ({resp.StatusCode}): {content}");
            return content;
        }
    }

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