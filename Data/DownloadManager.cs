using System;
using System.Collections.Concurrent;
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
        public event Action OnTransferFinished;

        public DownloadManager(SeafileClient client)
        {
            _client = client;
        }

        public void CancelAll() => _cts?.Cancel();

        // Speed Calc Variables
        private long _lastBytes = 0;
        private DateTime _lastUpdate = DateTime.MinValue;

        private void UpdateProgress(DownloadItem item, long currentBytes, long totalBytes, string statusPrefix)
        {
            // Speed Calculation
            DateTime now = DateTime.Now;
            if (_lastUpdate == DateTime.MinValue)
            {
                _lastUpdate = now;
                _lastBytes = currentBytes;
            }
            else if ((now - _lastUpdate).TotalSeconds >= 1)
            {
                double seconds = (now - _lastUpdate).TotalSeconds;
                long bytesDiff = currentBytes - _lastBytes;

                // Schutz vor negativen Werten bei Parallel-Uploads
                if (bytesDiff < 0) bytesDiff = 0;

                double speed = bytesDiff / seconds;
                item.SpeedString = UiHelper.FormatByteSize((long)speed) + "/s";

                _lastUpdate = now;
                _lastBytes = currentBytes;
            }

            int percent = totalBytes > 0 ? (int)((double)currentBytes / totalBytes * 100) : 0;
            if (percent > 100) percent = 100;

            item.Status = statusPrefix;
            item.Progress = percent;
            item.BytesTransferred = currentBytes;
            item.TotalSize = totalBytes;

            OnItemUpdated?.Invoke(item);
        }

        private void UpdateStatus(DownloadItem item, string status, int percent)
        {
            item.Status = status;
            item.Progress = percent;
            if (percent == 100) item.SpeedString = "Fertig";
            OnItemUpdated?.Invoke(item);
        }

        // ====================================================================================
        // UPLOAD LOGIC (NEU: Gruppiert)
        // ====================================================================================

        public async Task UploadFilesAsync(string[] filePaths, string repoId, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath)) targetPath = "/";

            // Ordner und Dateien trennen
            var directories = filePaths.Where(p => File.GetAttributes(p).HasFlag(FileAttributes.Directory)).ToArray();
            var files = filePaths.Where(p => !File.GetAttributes(p).HasFlag(FileAttributes.Directory)).ToArray();

            // 1. Einzelne Dateien normal hochladen (wie bisher)
            foreach (var file in files)
            {
                await UploadSingleFileAsync(file, repoId, targetPath);
            }

            // 2. Ordner als GRUPPE hochladen (NEU)
            foreach (var dir in directories)
            {
                await UploadFolderGroupedAsync(dir, repoId, targetPath);
            }
        }

        private async Task UploadSingleFileAsync(string localPath, string repoId, string targetPath)
        {
            string fileName = Path.GetFileName(localPath);
            string ext = Path.GetExtension(fileName).ToUpper().Replace(".", ""); // z.B. "PDF" oder "DOCX"
            if (string.IsNullOrEmpty(ext)) ext = "DATEI";

            var item = new DownloadItem
            {
                FileName = "⬆ " + fileName,
                Type = $"{ext} Upload", // <--- HIER: Zeigt jetzt "PDF Upload" an
                TotalSize = new FileInfo(localPath).Length,
                RemotePath = targetPath
            };

            History.Add(item);
            OnDownloadStarted?.Invoke(item);

            _lastBytes = 0;
            _lastUpdate = DateTime.MinValue;

            await StartTransferSafely(item, async () =>
            {
                UpdateStatus(item, "Hole Link...", 0);
                string link = await _client.GetUploadLinkAsync(repoId, targetPath);
                if (string.IsNullOrEmpty(link)) throw new Exception("Kein Upload-Link erhalten.");

                await _client.UploadFileWithProgressAsync(link, localPath, targetPath, fileName, (sent, total) =>
                {
                    UpdateProgress(item, sent, total, "Lade hoch...");
                });
            });
        }

        private async Task UploadFolderGroupedAsync(string localDirPath, string repoId, string remoteBasePath)
        {
            string folderName = new DirectoryInfo(localDirPath).Name;
            string targetRemoteDir = remoteBasePath.EndsWith("/") ? remoteBasePath + folderName : remoteBasePath + "/" + folderName;

            // 1. Scan: Alle Dateien finden & Größe berechnen
            var allFiles = Directory.GetFiles(localDirPath, "*", SearchOption.AllDirectories);
            long totalSize = 0;
            foreach (var f in allFiles) totalSize += new FileInfo(f).Length;

            // 2. Master Item erstellen & SubItems füllen (NEU)
            var masterItem = new DownloadItem
            {
                FileName = "⬆ 📁 " + folderName,
                Type = "Ordner Upload",
                TotalSize = totalSize,
                RemotePath = targetRemoteDir,
                Status = "Berechne..."
            };

            // Hier füllen wir die Detail-Liste für das Fenster
            foreach (var f in allFiles)
            {
                masterItem.SubItems.Add(new TransferSubItem
                {
                    Name = Path.GetFileName(f), // oder Path.GetRelativePath(...) für mehr Details
                    Status = "Wartet"
                });
            }

            History.Add(masterItem);
            OnDownloadStarted?.Invoke(masterItem);

            _lastBytes = 0;
            _lastUpdate = DateTime.MinValue;

            await StartTransferSafely(masterItem, async () =>
            {
                UpdateStatus(masterItem, "Erstelle Ordner...", 0);

                var allDirs = Directory.GetDirectories(localDirPath, "*", SearchOption.AllDirectories);
                try { await _client.CreateDirectoryAsync(repoId, targetRemoteDir); } catch { }

                var dirTasks = allDirs.Select(async dir =>
                {
                    string relPath = Path.GetRelativePath(localDirPath, dir).Replace("\\", "/");
                    string fullRemotePath = targetRemoteDir + "/" + relPath;
                    try { await _client.CreateDirectoryAsync(repoId, fullRemotePath); } catch { }
                });
                await Task.WhenAll(dirTasks);

                long globalBytesTransferred = 0;
                int filesUploaded = 0;
                int totalFiles = allFiles.Length;
                var fileProgressMap = new ConcurrentDictionary<string, long>();

                using (var semaphore = new SemaphoreSlim(3))
                {
                    var fileTasks = allFiles.Select(async filePath =>
                    {
                        await semaphore.WaitAsync();

                        // Finde das passende SubItem für das UI Update
                        var mySubItem = masterItem.SubItems.FirstOrDefault(x => x.Name == Path.GetFileName(filePath) && x.Status == "Wartet");
                        if (mySubItem != null) mySubItem.Status = "Lädt..."; // Status Update

                        try
                        {
                            string relPath = Path.GetRelativePath(localDirPath, filePath).Replace("\\", "/");
                            string fileName = Path.GetFileName(filePath);
                            string parentRelPath = Path.GetDirectoryName(relPath).Replace("\\", "/");
                            string remoteFileDir = targetRemoteDir;
                            if (!string.IsNullOrEmpty(parentRelPath)) remoteFileDir += "/" + parentRelPath;

                            string link = await _client.GetUploadLinkAsync(repoId, remoteFileDir);
                            fileProgressMap[filePath] = 0;

                            await _client.UploadFileWithProgressAsync(link, filePath, remoteFileDir, fileName, (sent, total) =>
                            {
                                long lastSent = fileProgressMap[filePath];
                                long delta = sent - lastSent;
                                fileProgressMap[filePath] = sent;
                                long newGlobal = Interlocked.Add(ref globalBytesTransferred, delta);
                                UpdateProgress(masterItem, newGlobal, totalSize, $"Upload {filesUploaded}/{totalFiles}");
                            });

                            if (mySubItem != null) mySubItem.Status = "✅ Fertig"; // Status Update
                            Interlocked.Increment(ref filesUploaded);
                        }
                        catch
                        {
                            if (mySubItem != null) mySubItem.Status = "❌ Fehler";
                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(fileTasks);
                }
            });
        }

        // ====================================================================================
        // DOWNLOAD LOGIC (Unverändert, aber included für Vollständigkeit)
        // ====================================================================================

        public async Task DownloadMultipleFilesAsZipAsync(List<object> items, string defaultRepoId, string defaultZipName)
        {
            string saveZipPath = GetSavePathOnMainThread(defaultZipName, "ZIP Archiv (*.zip)|*.zip");
            if (saveZipPath == null) return;

            var masterItem = new DownloadItem
            {
                FileName = "⬇ " + Path.GetFileName(saveZipPath),
                Type = "Batch Download"
            };
            History.Add(masterItem);
            OnDownloadStarted?.Invoke(masterItem);

            await StartTransferSafely(masterItem, async () =>
            {
                string tempRoot = Path.Combine(Path.GetTempPath(), "SeafileBatch_" + Guid.NewGuid().ToString().Substring(0, 8));
                Directory.CreateDirectory(tempRoot);

                try
                {
                    int totalItems = items.Count;
                    int processed = 0;

                    foreach (var obj in items)
                    {
                        string currentRepoId = defaultRepoId;
                        SeafileEntry entry = null;
                        SeafileRepo repo = null;

                        if (obj is Tuple<string, SeafileEntry> tuple) { currentRepoId = tuple.Item1; entry = tuple.Item2; }
                        else if (obj is SeafileEntry e) { entry = e; }
                        else if (obj is SeafileRepo r) { repo = r; currentRepoId = r.id; }

                        if (repo != null)
                        {
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
                                string link = await _client.GetDownloadLinkAsync(currentRepoId, fullEntryPath);
                                await _client.DownloadFileWithProgressAsync(link, localDest, null);
                            }
                        }
                        processed++;
                        UpdateStatus(masterItem, $"Lade {processed}/{totalItems}...", (int)((double)processed / totalItems * 80));
                    }
                    UpdateStatus(masterItem, "Erstelle ZIP...", 90);
                    if (File.Exists(saveZipPath)) File.Delete(saveZipPath);
                    ZipFile.CreateFromDirectory(tempRoot, saveZipPath);
                }
                finally
                {
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
                else await DownloadSingleFileAsync(item, entry, repoId, fullPath); // Call overload with item
            });
        }

        private async Task DownloadSingleFileAsync(DownloadItem item, SeafileEntry entry, string repoId, string fullPath)
        {
            string savePath = GetSavePathOnMainThread(entry.name, "Alle Dateien (*.*)|*.*");
            if (savePath == null) throw new OperationCanceledException();

            item.TotalSize = entry.size;
            _lastBytes = 0;
            _lastUpdate = DateTime.MinValue;

            UpdateStatus(item, "Hole Link...", 0);
            string link = await _client.GetDownloadLinkAsync(repoId, fullPath);
            await _client.DownloadFileWithProgressAsync(link, savePath, (curr, total) =>
            {
                UpdateProgress(item, curr, total, "Lade Datei...");
            });
        }

        private async Task DownloadFolderSmartAsync(DownloadItem item, SeafileEntry entry, string repoId, string fullPath)
        {
            string savePath = GetSavePathOnMainThread(entry.name + ".zip", "ZIP Archiv (*.zip)|*.zip");
            if (savePath == null) throw new OperationCanceledException();

            bool serverZipFailed = false;
            try
            {
                UpdateStatus(item, "Server packt...", 0);
                string zipLink = await _client.GetDirectoryZipLinkAsync(repoId, fullPath);

                _lastBytes = 0;
                _lastUpdate = DateTime.MinValue;
                await _client.DownloadFileWithProgressAsync(zipLink, savePath, (curr, total) =>
                {
                    UpdateProgress(item, curr, total, "Lade ZIP...");
                });
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
                        string fullFilePathOnServer = file.parent_dir.EndsWith("/") ? file.parent_dir + file.name : file.parent_dir + "/" + file.name;
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
                            UpdateStatus(item, $"Turbo: {processed}/{files.Count}", (int)((double)processed / files.Count * 100));
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
                UpdateStatus(item, "Fertig", 100);
            }
            catch (Exception ex)
            {
                item.ErrorMessage = ex.Message;
                UpdateStatus(item, "Fehler", 0);

                var mainForm = Application.OpenForms.OfType<Form>().FirstOrDefault();
                if (mainForm != null && mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(() => UiHelper.ShowScrollableErrorDialog("Transfer-Fehler", $"Objekt: {item.FileName}\n\nFehler: {ex.Message}")));
                }
                else
                {
                    UiHelper.ShowScrollableErrorDialog("Transfer-Fehler", $"Objekt: {item.FileName}\n\nFehler: {ex.Message}");
                }
            }
            finally
            {
                OnTransferFinished?.Invoke();

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