using System.Linq;

namespace WinFormsApp3.Data
{
    public class NavigationState
    {
        public string CurrentRepoId { get; private set; } = null;
        public string CurrentRepoName { get; private set; } = "";
        public string CurrentPath { get; private set; } = "/";

        public bool IsInRoot => CurrentRepoId == null;

        public void ResetToRoot()
        {
            CurrentRepoId = null;
            CurrentRepoName = "";
            CurrentPath = "/";
        }

        public void EnterRepo(string repoId, string repoName)
        {
            CurrentRepoId = repoId;
            CurrentRepoName = repoName;
            CurrentPath = "/";
        }

        public void EnterFolder(string folderName)
        {
            if (CurrentPath.EndsWith("/"))
                CurrentPath += folderName;
            else
                CurrentPath += "/" + folderName;
        }

        // NEU: Ermöglicht das direkte Springen zu einem Pfad (für "Gehe zu")
        public void SetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                CurrentPath = "/";
                return;
            }

            // Sicherstellen, dass Format stimmt (kein Slash am Ende, außer es ist Root)
            string clean = fullPath.Replace("\\", "/").Trim();
            if (clean.Length > 1 && clean.EndsWith("/"))
                clean = clean.TrimEnd('/');

            if (!clean.StartsWith("/"))
                clean = "/" + clean;

            CurrentPath = clean;
        }

        public void GoBack()
        {
            if (CurrentPath == "/" || string.IsNullOrEmpty(CurrentPath))
            {
                ResetToRoot();
            }
            else
            {
                int lastSlash = CurrentPath.LastIndexOf('/');
                if (lastSlash <= 0) // z.B. "/Ordner" -> lastSlash=0
                    CurrentPath = "/";
                else
                    CurrentPath = CurrentPath.Substring(0, lastSlash);
            }
        }
    }
}