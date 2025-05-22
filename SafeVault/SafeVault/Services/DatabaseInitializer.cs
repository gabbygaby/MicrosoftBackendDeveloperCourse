using MySql.Data.MySqlClient;
using System;
using System.IO;

namespace SafeVault.Services
{
    public class DatabaseInitializer
    {
        public void Initialize(string connectionString)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = File.ReadAllText("database.sql"); // Ensure the path is correct
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Database initialized successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }
    }
}