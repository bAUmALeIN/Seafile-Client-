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
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );";
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveToken(string token)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ('api_token', @token);";
                cmd.Parameters.AddWithValue("@token", token);
                cmd.ExecuteNonQuery();
            }
        }

        public string GetToken()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'api_token' LIMIT 1;";
                    var result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
            catch { return null; }
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
    }
}