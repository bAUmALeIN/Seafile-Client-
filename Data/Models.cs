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

    }

    public class SeafileEntry
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; } 
        public long size { get; set; }
        public long mtime { get; set; }
    }
}
