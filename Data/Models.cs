using System;
using System.Collections.Generic;

namespace WinFormsApp3.Data
{
    public static class AppConfig
    {
        private const string DefaultApiUrl = "https://seafile.bbs-me.org/api2/";
        private const string DefaultLoginUrl = "https://bbs-me.org/app/Seafile/12";

        public static string ApiBaseUrl { get; set; } = DefaultApiUrl;
        public static string LoginUrl { get; set; } = DefaultLoginUrl;
        public static string DbConnectionString { get; } = "Data Source=seafile_data.db";
        public static string WebViewUserDataFolder { get; } = "WebView2_UserData";
        public static string CSRFToken { get; set; } = "";
        public static string RawCookies { get; set; } = "";

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
        public string type { get; set; }
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
        public string Type { get; set; }
        public long TotalSize { get; set; }
        public long BytesTransferred { get; set; }
        public string Status { get; set; } = "Wartet...";
        public int Progress { get; set; } = 0;
        public string SpeedString { get; set; } = "-";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public string ErrorMessage { get; set; }
        public string LocalFilePath { get; set; }

        // NEU: Zielpfad auf dem Server
        public string RemotePath { get; set; }

        public object Tag { get; set; }
    }
}