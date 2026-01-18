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

        public List<DownloadItem> History { get; private set; } = new List<DownloadItem>();

        public event Action<DownloadItem> OnItemUpdated;
        public event Action<DownloadItem> OnDownloadStarted;

        public DownloadManager(SeafileClient client)
        {
            _client = client;
        }

        public void CancelAll() => _cts?.Cancel();

        private void UpdateItem(DownloadItem item, string status, int progress)
        {
            item.Status = status;
            item.Progress = progress;
            OnItemUpdated?.Invoke(item);
        }

        // =========================================================
        // UPLOAD LOGIK
        // =========================================================
        public async Task UploadFilesAsync(string[] filePaths, string repoId, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath)) targetPath = "/";

            foreach (var path in filePaths)
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                    await UploadFolderRecursiveAsync(path, repoId, targetPath);
                else
                    await UploadSingleFileAsync(path, repoId, targetPath);
            }
        }

        private async Task UploadSingleFileAsync(string localPath, string repoId, string targetPath)
        {
            string fileName = Path.GetFileName(localPath);
            var item = new DownloadItem { FileName = "⬆ " + fileName, Type = "Upload" };
            History.Add(item);
            OnDownloadStarted?.Invoke(item);

            await StartTransferSafely(item, async () =>
            {
                UpdateItem(item, "Hole Link...", 0);
                string link = await _client.GetUploadLinkAsync(repoId, targetPath);
                if (string.IsNullOrEmpty(link)) throw new Exception("Kein Upload-Link erhalten.");

                await _client.UploadFileWithProgressAsync(link, localPath, targetPath, fileName, (percent) =>
                {
                    UpdateItem(item, "Lade hoch...", percent);
                });
            });
        }

        private async Task UploadFolderRecursiveAsync(string localDir, string repoId, string remoteBasePath)
        {
            string folderName = new DirectoryInfo(localDir).Name;
            string newRemotePath = remoteBasePath.EndsWith("/") ? remoteBasePath + folderName : remoteBasePath + "/" + folderName;

            try { await _client.CreateDirectoryAsync(repoId, newRemotePath); } catch { }

            string[] files = Directory.GetFiles(localDir);
            using (var semaphore = new SemaphoreSlim(3))
            {
                var tasks = files.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try { await UploadSingleFileAsync(file, repoId, newRemotePath); }
                    finally { semaphore.Release(); }
                });
                await Task.WhenAll(tasks);
            }

            foreach (string subDir in Directory.GetDirectories(localDir))
            {
                await UploadFolderRecursiveAsync(subDir, repoId, newRemotePath);
            }
        }

        // =========================================================
        // DOWNLOAD LOGIK
        // =========================================================

        // NEU: Batch Download (Alles in eine ZIP)
        public async Task DownloadMultipleFilesAsZipAsync(List<object> items, string defaultRepoId, string defaultZipName)
        {
            // 1. Pfad abfragen (Nur EIN Dialog)
            string saveZipPath = GetSavePathOnMainThread(defaultZipName, "ZIP Archiv (*.zip)|*.zip");
            if (saveZipPath == null) return; // User hat abgebrochen

            // 2. Download Item erstellen
            var masterItem = new DownloadItem
            {
                FileName = "⬇ " + Path.GetFileName(saveZipPath),
                Type = "Batch Download"
            };
            History.Add(masterItem);
            OnDownloadStarted?.Invoke(masterItem);

            await StartTransferSafely(masterItem, async () =>
            {
                // Temporäres Verzeichnis erstellen
                string tempRoot = Path.Combine(Path.GetTempPath(), "SeafileBatch_" + Guid.NewGuid().ToString().Substring(0, 8));
                Directory.CreateDirectory(tempRoot);

                try
                {
                    int totalItems = items.Count;
                    int processed = 0;

                    foreach (var obj in items)
                    {
                        // RepoID und Pfad ermitteln (Da Suche verschiedene Repos haben kann)
                        string currentRepoId = defaultRepoId;
                        SeafileEntry entry = null;
                        SeafileRepo repo = null;

                        if (obj is Tuple<string, SeafileEntry> tuple)
                        {
                            currentRepoId = tuple.Item1;
                            entry = tuple.Item2;
                        }
                        else if (obj is SeafileEntry e)
                        {
                            entry = e;
                        }
                        else if (obj is SeafileRepo r)
                        {
                            repo = r;
                            currentRepoId = r.id;
                        }

                        // --- Verarbeitung ---
                        if (repo != null)
                        {
                            // Ganze Bibliothek laden (als Ordner im Temp)
                            string targetDir = Path.Combine(tempRoot, repo.name);
                            Directory.CreateDirectory(targetDir);
                            await DownloadFolderContentsRecursively(masterItem, currentRepoId, "/", targetDir, false);
                        }
                        else if (entry != null)
                        {
                            string fullEntryPath = (entry.parent_dir ?? "/").TrimEnd('/') + "/" + entry.name;
                            string localDest = Path.Combine(tempRoot, entry.name);

                            if (entry.type == "dir")
                            {
                                Directory.CreateDirectory(localDest);
                                await DownloadFolderContentsRecursively(masterItem, currentRepoId, fullEntryPath, localDest, false);
                            }
                            else
                            {
                                // Einzeldatei laden
                                string link = await _client.GetDownloadLinkAsync(currentRepoId, fullEntryPath);
                                await _client.DownloadFileWithProgressAsync(link, localDest, null);
                            }
                        }

                        processed++;
                        UpdateItem(masterItem, $"Lade {processed}/{totalItems}...", (int)((double)processed / totalItems * 80));
                    }

                    // Alles Zippen
                    UpdateItem(masterItem, "Erstelle ZIP...", 90);
                    if (File.Exists(saveZipPath)) File.Delete(saveZipPath);
                    ZipFile.CreateFromDirectory(tempRoot, saveZipPath);

                }
                finally
                {
                    // Aufräumen
                    if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
                }
            });
        }

        public async Task DownloadRepoAsync(SeafileRepo repo)
        {
            var item = new DownloadItem { FileName = "⬇ " + repo.name, Type = "Bibliothek" };
            History.Add(item);
            OnDownloadStarted?.Invoke(item);

            await StartTransferSafely(item, async () =>
            {
                var dummyEntry = new SeafileEntry { name = repo.name, type = "dir" };
                await DownloadFolderSmartAsync(item, dummyEntry, repo.id, "/");
            });
        }

        public async Task DownloadEntryAsync(SeafileEntry entry, string repoId, string currentNavPath)
        {
            var item = new DownloadItem { FileName = "⬇ " + entry.name, Type = entry.type == "dir" ? "Ordner" : "Datei" };
            History.Add(item);
            OnDownloadStarted?.Invoke(item);
            string fullPath = BuildFullPath(entry, currentNavPath);

            await StartTransferSafely(item, async () =>
            {
                if (entry.type == "dir") await DownloadFolderSmartAsync(item, entry, repoId, fullPath);
                else await DownloadSingleFileAsync(item, entry, repoId, fullPath);
            });
        }

        private async Task DownloadSingleFileAsync(DownloadItem item, SeafileEntry entry, string repoId, string fullPath)
        {
            string savePath = GetSavePathOnMainThread(entry.name, "Alle Dateien (*.*)|*.*");
            if (savePath == null) throw new OperationCanceledException();

            UpdateItem(item, "Hole Link...", 5);
            string link = await _client.GetDownloadLinkAsync(repoId, fullPath);

            await _client.DownloadFileWithProgressAsync(link, savePath, (percent) =>
            {
                UpdateItem(item, "Lade Datei...", percent);
            });
        }

        private async Task DownloadFolderSmartAsync(DownloadItem item, SeafileEntry entry, string repoId, string fullPath)
        {
            string savePath = GetSavePathOnMainThread(entry.name + ".zip", "ZIP Archiv (*.zip)|*.zip");
            if (savePath == null) throw new OperationCanceledException();

            bool serverZipFailed = false;
            try
            {
                UpdateItem(item, "Server packt...", 10);
                string zipLink = await _client.GetDirectoryZipLinkAsync(repoId, fullPath);
                await _client.DownloadFileWithProgressAsync(zipLink, savePath, (p) => UpdateItem(item, "Lade ZIP...", p));
            }
            catch
            {
                serverZipFailed = true;
            }

            if (serverZipFailed)
            {
                item.Type = "Ordner (Turbo)";
                await DownloadFolderClientSideAsync(item, repoId, fullPath, savePath);
            }
        }

        private async Task DownloadFolderClientSideAsync(DownloadItem item, string repoId, string folderPath, string targetZipPath)
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "Seafile_" + Guid.NewGuid().ToString().Substring(0, 8));
            Directory.CreateDirectory(tempRoot);

            try
            {
                await DownloadFolderContentsRecursively(item, repoId, folderPath, tempRoot, true);

                if (File.Exists(targetZipPath)) File.Delete(targetZipPath);
                ZipFile.CreateFromDirectory(tempRoot, targetZipPath);
            }
            finally { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true); }
        }

        private async Task DownloadFolderContentsRecursively(DownloadItem item, string repoId, string serverFolderPath, string localTargetDir, bool updateProgress)
        {
            var allEntries = await _client.GetAllFilesRecursiveAsync(repoId, serverFolderPath);
            var files = allEntries.Where(x => x.type == "file").ToList();
            int processed = 0;

            using (var semaphore = new SemaphoreSlim(5))
            {
                var tasks = files.Select(async file => {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Pfad Berechnung
                        string fullFilePathOnServer = file.parent_dir.EndsWith("/")
                            ? file.parent_dir + file.name
                            : file.parent_dir + "/" + file.name;

                        string relativePath = fullFilePathOnServer;
                        if (serverFolderPath != "/" && fullFilePathOnServer.StartsWith(serverFolderPath))
                        {
                            relativePath = fullFilePathOnServer.Substring(serverFolderPath.Length);
                        }

                        string relativeLocalPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
                        string localDest = Path.Combine(localTargetDir, relativeLocalPath);

                        Directory.CreateDirectory(Path.GetDirectoryName(localDest));

                        string link = await _client.GetDownloadLinkAsync(repoId, fullFilePathOnServer);
                        await _client.DownloadFileWithProgressAsync(link, localDest, null);

                        Interlocked.Increment(ref processed);
                        if (updateProgress)
                        {
                            UpdateItem(item, $"Turbo: {processed}/{files.Count}", (int)((double)processed / files.Count * 100));
                        }
                    }
                    finally { semaphore.Release(); }
                });
                await Task.WhenAll(tasks);
            }
        }

        private async Task StartTransferSafely(DownloadItem item, Func<Task> action)
        {
            _cts = new CancellationTokenSource();
            try
            {
                await action();
                UpdateItem(item, "Fertig", 100);
            }
            catch (Exception ex)
            {
                item.ErrorMessage = ex.Message;
                UpdateItem(item, "Fehler", 0);

                var mainForm = Application.OpenForms.OfType<Form>().FirstOrDefault();
                if (mainForm != null && mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(() =>
                        UiHelper.ShowScrollableErrorDialog("Transfer-Fehler", $"Objekt: {item.FileName}\n\nFehler: {ex.Message}")
                    ));
                }
                else
                {
                    UiHelper.ShowScrollableErrorDialog("Transfer-Fehler", $"Objekt: {item.FileName}\n\nFehler: {ex.Message}");
                }
            }
        }

        private string GetSavePathOnMainThread(string defaultName, string filter)
        {
            string savePath = null;
            var mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            Action action = () =>
            {
                SaveFileDialog sfd = new SaveFileDialog { FileName = defaultName, Filter = filter };
                if (sfd.ShowDialog() == DialogResult.OK) savePath = sfd.FileName;
            };

            if (mainForm != null && mainForm.InvokeRequired) mainForm.Invoke(action);
            else action();

            return savePath;
        }

        private string BuildFullPath(SeafileEntry entry, string currentNavPath)
        {
            string basePath = currentNavPath.EndsWith("/") ? currentNavPath : currentNavPath + "/";
            return basePath + entry.name;
        }
    }
}