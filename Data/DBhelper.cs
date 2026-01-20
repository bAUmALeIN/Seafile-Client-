using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography; // Wichtig für DPAPI
using System.Text;

namespace WinFormsApp3.Data
{
    public class DBHelper
    {
        private readonly string _connectionString;

        public DBHelper()
        {
            _connectionString = AppConfig.DbConnectionString;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );";
                cmd.ExecuteNonQuery();
            }
        }

        // --- Generische Methoden (Unverschlüsselt für Settings wie URL) ---

        public void SaveSetting(string key, string value)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@key, @value);";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@value", value ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        public string GetSetting(string key)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @key LIMIT 1;";
                    cmd.Parameters.AddWithValue("@key", key);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
            catch { return null; }
        }

        // --- Token Management (Jetzt Verschlüsselt!) ---

        public void SaveToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            // Vor dem Speichern verschlüsseln
            string encryptedToken = EncryptString(token);
            SaveSetting(AppConfig.SettingsKeys.ApiToken, encryptedToken);
        }

        public string GetToken()
        {
            string encryptedToken = GetSetting(AppConfig.SettingsKeys.ApiToken);
            if (string.IsNullOrEmpty(encryptedToken)) return null;

            // Nach dem Laden entschlüsseln
            return DecryptString(encryptedToken);
        }

        public void DeleteToken()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Settings WHERE Key = 'api_token';";
                cmd.ExecuteNonQuery();
            }
        }

        // --- HELPER: WINDOWS DPAPI VERSCHLÜSSELUNG ---

        private string EncryptString(string plainText)
        {
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                // DataProtectionScope.CurrentUser -> Nur der User, der eingeloggt ist, kann es entschlüsseln
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string DecryptString(string cipherText)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception)
            {
                // Falls Entschlüsselung fehlschlägt (z.B. altes unverschlüsseltes Token oder anderer PC)
                // Geben wir null zurück, damit der User sich neu einloggen muss.
                return null;
            }
        }
    }
}