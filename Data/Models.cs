using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace WinFormsApp3.Data
{

    public static class AppConfig
    {
        public const string ApiBaseUrl = "https://seafile.bbs-me.org/api2/";
        public const string DbConnectionString = "Data Source=seafile_data.db";
        public const string WebViewUserDataFolder = "WebView2_UserData";
        public const string LoginUrl = "https://bbs-me.org/app/Seafile/12";
    }

    public class SeafileRepo
    {
        public string id { get; set; }
        public string name { get; set; }
        public long size { get; set; }
        public long mtime { get; set; }

        public string type { get; set; } // "repo", "srepo" (freigegeben), "grepo" (Gruppe)
        public string owner { get; set; }

    }

    public class SeafileEntry
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; } 
        public long size { get; set; }
        public long mtime { get; set; }
        public string parent_dir { get; set; }
    }

    public class DownloadItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Eindeutige ID
        public string FileName { get; set; }
        public string Type { get; set; } // "Datei", "Ordner (Zip)", "Ordner (Turbo)"
        public long TotalSize { get; set; } // Optional
        public string Status { get; set; } = "Wartet..."; // "Lädt...", "Fertig", "Fehler"
        public int Progress { get; set; } = 0; // 0 bis 100
        public DateTime StartTime { get; set; } = DateTime.Now;

        // Hilfsfeld für UI-Update (damit wir wissen, welches ListViewItem dazu gehört)
        public object Tag { get; set; }
    }

}
