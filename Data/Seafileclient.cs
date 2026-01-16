using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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
        }

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


        public async Task<bool> CreateDirectoryAsync(string repoId, string path)
        {
            // Seafile API: POST /api2/repos/{repo_id}/dir/?p={path} mit operation=mkdir
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
            // Unterscheidung API Endpunkt: 'dir' oder 'file'
            string type = isDir ? "dir" : "file";

            string url = $"{AppConfig.ApiBaseUrl}repos/{repoId}/{type}/?p={encodedPath}";

            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}