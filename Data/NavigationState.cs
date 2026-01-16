using System;

namespace WinFormsApp3.Data
{
    
    public class NavigationState
    {
        public string CurrentRepoId { get; private set; } = null;
        public string CurrentPath { get; private set; } = "/";

        public bool IsInRoot => CurrentRepoId == null;

        
        public void ResetToRoot()
        {
            CurrentRepoId = null;
            CurrentPath = "/";
        }

        
        public void EnterRepo(string repoId)
        {
            CurrentRepoId = repoId;
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