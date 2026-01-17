using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class DownloadManager
    {
        private readonly SeafileClient _client;
        private CancellationTokenSource _cts;

        // Die Liste aller Downloads in dieser Session
        public List<DownloadItem> History { get; private set; } = new List<DownloadItem>();

        // Events für die GUI
        public event Action<DownloadItem> OnItemUpdated;      // Status/Prozent geändert
        public event Action<DownloadItem> OnDownloadStarted;  // Neuer Download hinzugefügt

        public DownloadManager(SeafileClient client)
        {
            _client = client;
        }

        public void CancelAll()
        {
            _cts?.Cancel();
        }

        // Helper um Updates an die UI zu feuern
        private void UpdateItem(DownloadItem item, string status, int progress)
        {
            item.Status = status;
            item.Progress = progress;
            OnItemUpdated?.Invoke(item);
        }

        // =========================================================
        // START METHODEN
        // =========================================================

        public async Task DownloadRepoAsync(SeafileRepo repo)
        {
            var item = new DownloadItem { FileName = repo.name, Type = "Bibliothek" };
            History.Add(item);
            OnDownloadStarted?.Invoke(item);

            var dummyEntry = new SeafileEntry { name = repo.name, type = "dir" };
            await StartDownloadSafely(item, () => DownloadFolderSmartAsync(item, dummyEntry, repo.id, "/"));
        }

        public async Task DownloadEntryAsync(SeafileEntry entry, string repoId, string currentNavPath)
        {
            var item = new DownloadItem { FileName = entry.name, Type = entry.type == "dir" ? "Ordner" : "Datei" };
            History.Add(item);
            OnDownloadStarted?.Invoke(item);

            string fullPath = BuildFullPath(entry, currentNavPath);

            await StartDownloadSafely(item, async () =>
            {
                if (entry.type == "dir")
                    await DownloadFolderSmartAsync(item, entry, repoId, fullPath);
                else
                    await DownloadSingleFileAsync(item, entry, repoId, fullPath);
            });
        }

        // =========================================================
        // LOGIK
        // =========================================================

        private async Task StartDownloadSafely(DownloadItem item, Func<Task> downloadAction)
        {
            _cts = new CancellationTokenSource();
            try
            {
                UpdateItem(item, "Initialisiere...", 0);
                await downloadAction();
                UpdateItem(item, "Fertig", 100);
            }
            catch (OperationCanceledException)
            {
                UpdateItem(item, "Abgebrochen", 0);
            }
            catch (Exception ex)
            {
                UpdateItem(item, "Fehler: " + ex.Message, 0);
                UiHelper.ShowScrollableErrorDialog("Download Fehler", ex.Message);
            }
        }

        private async Task DownloadSingleFileAsync(DownloadItem item, SeafileEntry entry, string repoId, string fullPath)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = entry.name;
            sfd.Filter = "Alle Dateien (*.*)|*.*";

            if (sfd.ShowDialog() != DialogResult.OK) throw new OperationCanceledException();

            UpdateItem(item, "Hole Link...", 10);
            string link = await _client.GetDownloadLinkAsync(repoId, fullPath);

            _cts.Token.ThrowIfCancellationRequested();

            UpdateItem(item, "Lade Datei...", 50);
            await _client.DownloadFileAsStreamAsync(link, sfd.FileName);

            ValidateFile(sfd.FileName);
        }

        private async Task DownloadFolderSmartAsync(DownloadItem item, SeafileEntry entry, string repoId, string fullPath)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = entry.name + ".zip";
            sfd.Filter = "ZIP Archiv (*.zip)|*.zip";

            if (sfd.ShowDialog() != DialogResult.OK) throw new OperationCanceledException();

            bool serverZipFailed = false;

            // VERSUCH 1: Server-ZIP
            try
            {
                UpdateItem(item, "Server packt ZIP...", 20);
                string zipLink = await _client.GetDirectoryZipLinkAsync(repoId, fullPath);

                _cts.Token.ThrowIfCancellationRequested();

                UpdateItem(item, "Lade ZIP...", 60);
                await _client.DownloadFileAsStreamAsync(zipLink, sfd.FileName);

                VerifyZipIntegrity(sfd.FileName);
                CheckAndFixFileFormat(sfd.FileName);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) throw;
                serverZipFailed = true;
                UpdateItem(item, $"Server-ZIP Fehler. Starte Turbo...", 0);
            }

            // VERSUCH 2: Fallback (Turbo)
            if (serverZipFailed)
            {
                item.Type = "Ordner (Turbo)";
                await DownloadFolderClientSideAsync(item, repoId, fullPath, sfd.FileName);
            }
        }

        private async Task DownloadFolderClientSideAsync(DownloadItem item, string repoId, string folderPath, string targetZipPath)
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "Seafile_" + Guid.NewGuid().ToString().Substring(0, 8));
            Directory.CreateDirectory(tempRoot);

            try
            {
                UpdateItem(item, "Analysiere Struktur...", 5);
                var allEntries = await _client.GetAllFilesRecursiveAsync(repoId, folderPath);
                var files = allEntries.Where(x => x.type == "file").ToList();

                if (files.Count == 0) throw new Exception("Ordner ist leer.");

                int processedCount = 0;
                int totalFiles = files.Count;

                using (var semaphore = new SemaphoreSlim(5))
                {
                    var tasks = files.Select(async file =>
                    {
                        await semaphore.WaitAsync(_cts.Token);
                        try
                        {
                            _cts.Token.ThrowIfCancellationRequested();

                            // Pfad Logik
                            string relativePath = "";
                            string fullFilePath = file.parent_dir.EndsWith("/") ? file.parent_dir + file.name : file.parent_dir + "/" + file.name;

                            if (folderPath == "/") relativePath = fullFilePath.TrimStart('/');
                            else if (fullFilePath.StartsWith(folderPath)) relativePath = fullFilePath.Substring(folderPath.Length).TrimStart('/');
                            else relativePath = file.name;

                            relativePath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString());
                            string localDestPath = Path.Combine(tempRoot, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(localDestPath));

                            string link = await _client.GetDownloadLinkAsync(repoId, fullFilePath);
                            await _client.DownloadFileAsStreamAsync(link, localDestPath);

                            int current = Interlocked.Increment(ref processedCount);
                            int percent = (int)((double)current / totalFiles * 90);

                            // UI Update gedrosselt
                            if (current % 2 == 0 || current == totalFiles)
                            {
                                UpdateItem(item, $"Lade {current}/{totalFiles}: {file.name}", percent);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                }

                UpdateItem(item, "Erstelle ZIP...", 95);
                await Task.Delay(500);

                if (File.Exists(targetZipPath)) File.Delete(targetZipPath);
                ZipFile.CreateFromDirectory(tempRoot, targetZipPath);
            }
            finally
            {
                try { Directory.Delete(tempRoot, true); } catch { }
            }
        }

        // --- Helper ---
        private void VerifyZipIntegrity(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Length < 22) throw new Exception("Zu klein");
                using (ZipArchive archive = ZipFile.OpenRead(filePath)) { var c = archive.Entries.Count; }
            }
            catch { throw new Exception("ZIP defekt"); }
        }

        private void ValidateFile(string path)
        {
            FileInfo fi = new FileInfo(path);
            if (fi.Length == 0) throw new Exception("0 Byte");
            if (fi.Length < 2000)
            {
                try
                {
                    string c = File.ReadAllText(path).Trim();
                    if (c.StartsWith("<") && c.Contains("html")) throw new Exception("HTML Fehler");
                }
                catch { }
            }
        }

        private string CheckAndFixFileFormat(string filePath) { return filePath; }

        private string BuildFullPath(SeafileEntry entry, string currentNavPath)
        {
            if (!string.IsNullOrEmpty(entry.parent_dir) && entry.parent_dir != "/")
            {
                string pDir = entry.parent_dir.Trim();
                if (!pDir.StartsWith("/")) pDir = "/" + pDir;
                if (!pDir.EndsWith("/")) pDir += "/";
                return pDir + entry.name;
            }
            string basePath = currentNavPath;
            if (!basePath.EndsWith("/")) basePath += "/";
            return basePath + entry.name;
        }
    }
}