namespace WinFormsApp3.Data
{
    public static class AppConstants
    {
        // SQL Queries
        public static class Sql
        {
            public const string TableName = "Settings";

            public const string CreateTable = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";

            public const string InsertToken = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ('api_token', @token);";
            public const string SelectToken = "SELECT Value FROM Settings WHERE Key = 'api_token' LIMIT 1;";
            public const string DeleteToken = "DELETE FROM Settings WHERE Key = 'api_token';";
        }

        // JavaScript Scripte
        public static class Scripts
        {
            public const string SeafileAutoClicker = @"
                (function() {
                    if (window.seafileAutoClickerRunning) return;
                    window.seafileAutoClickerRunning = true;
                    console.log('Auto-Clicker gestartet für: ' + window.location.href);

                    var attempts = 0;
                    var maxAttempts = 60; // 30 Sekunden lang probieren
                    
                    function tryClickSeafile() {
                        attempts++;
                        var apps = document.querySelectorAll('.appBubbleClass');
                        if (apps.length === 0) return false;

                        for(var i = 0; i < apps.length; i++) {
                            var text = (apps[i].innerText || '').toLowerCase();
                            if(text.includes('seafile')) {
                                console.log('Seafile gefunden! Klicke...');
                                var btn = apps[i].querySelector('button');
                                if(btn) { btn.click(); return true; }
                                apps[i].click(); 
                                return true;
                            }
                        }
                        return false;
                    }

                    var intervalId = setInterval(function() {
                        var success = tryClickSeafile();
                        if (success || attempts >= maxAttempts) {
                            clearInterval(intervalId);
                            window.seafileAutoClickerRunning = false; 
                        }
                    }, 500);
                })();
            ";
        }
    }
}