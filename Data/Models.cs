using System;
using System.Collections.Generic;

namespace WinFormsApp3.Data
{
    public static class AppConfig
    {
        // Standardwerte (Falls DB leer ist)
        private const string DefaultApiUrl = "https://seafile.bbs-me.org/api2/";
        private const string DefaultLoginUrl = "https://bbs-me.org/app/Seafile/12";

        // Dynamische Properties - werden zur Laufzeit geändert/geladen
        public static string ApiBaseUrl { get; set; } = DefaultApiUrl;
        public static string LoginUrl { get; set; } = DefaultLoginUrl;

        // Konstanten, die sich nicht ändern
        public static string DbConnectionString { get; } = "Data Source=seafile_data.db";
        public static string WebViewUserDataFolder { get; } = "WebView2_UserData";

        // Keys für die Datenbank-Tabelle "Settings"
        public static class SettingsKeys
        {
            public const string ApiUrl = "api_url";
            public const string LoginUrl = "login_url";
            public const string ApiToken = "api_token";
        }
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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; }
        public string Type { get; set; } // "Upload", "Download", etc.
        public long TotalSize { get; set; }
        public string Status { get; set; } = "Wartet...";
        public int Progress { get; set; } = 0;
        public DateTime StartTime { get; set; } = DateTime.Now;

        // Speichert die Fehlermeldung für die Detailansicht
        public string ErrorMessage { get; set; }

        public object Tag { get; set; }
    }
}