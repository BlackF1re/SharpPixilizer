using System;
using Microsoft.Data.Sqlite;

namespace SharpPixilizer
{
    public class SettingsManager
    {
        private readonly string connectionString;

        public SettingsManager(string dbPath)
        {
            connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        // Создаем таблицу, если её нет
        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Settings (
                            [Key] TEXT PRIMARY KEY,
                            [Value] TEXT NOT NULL
                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        // Сохранение или обновление настройки
        public void SaveSetting(string key, string value)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Используем INSERT OR REPLACE для обновления
                    command.CommandText = @"
                        INSERT OR REPLACE INTO Settings ([Key], [Value])
                        VALUES (@key, @value);";
                    command.Parameters.AddWithValue("@key", key);
                    command.Parameters.AddWithValue("@value", value);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Получение настройки (если нет — возвращаем null)
        public string GetSetting(string key)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT [Value] FROM Settings WHERE [Key] = @key;";
                    command.Parameters.AddWithValue("@key", key);
                    var result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }
    }
}
