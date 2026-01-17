namespace WinFormsApp3.Data
{
    public class NavigationState
    {
        public string CurrentRepoId { get; private set; } = null;
        public string CurrentRepoName { get; private set; } = ""; // NEU
        public string CurrentPath { get; private set; } = "/";

        public bool IsInRoot => CurrentRepoId == null;

        public void ResetToRoot()
        {
            CurrentRepoId = null;
            CurrentRepoName = ""; // Reset
            CurrentPath = "/";
        }

        // NEU: Wir übergeben jetzt auch den Namen
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

        public void GoBack()
        {
            if (CurrentPath == "/")
            {
                ResetToRoot();
            }
            else
            {
                int lastSlash = CurrentPath.LastIndexOf('/');
                if (lastSlash <= 0)
                    CurrentPath = "/";
                else
                    CurrentPath = CurrentPath.Substring(0, lastSlash);
            }
        }
    }
}