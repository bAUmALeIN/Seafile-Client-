using Microsoft.Data.Sqlite;
using System;

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
                // Tabelle ist generisch key/value
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );";
                cmd.ExecuteNonQuery();
            }
        }

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

        // Wrapper für Token (für Kompatibilität mit altem Code)
        public void SaveToken(string token) => SaveSetting(AppConfig.SettingsKeys.ApiToken, token);
        public string GetToken() => GetSetting(AppConfig.SettingsKeys.ApiToken);
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
    }
}