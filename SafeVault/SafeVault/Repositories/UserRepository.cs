using System;
using MySql.Data.MySqlClient;
using SafeVault.Models;
using SafeVault.Helpers;

namespace SafeVault.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public User GetUser(string username)
        {
            try
            {
                username = InputSanitizer.SanitizeInput(username);

                User user = null;
                string query = "SELECT UserID, Username, Email, PasswordHash, Role"
                + " FROM Users WHERE Username = @username;";

                using (var connection = new MySqlConnection(_connectionString))
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    UserID = reader.GetInt32("UserID"),
                                    Username = reader.GetString("Username"),
                                    Email = reader.GetString("Email"),
                                    PasswordHash = reader.GetString("PasswordHash"),
                                    Role = reader.GetString("Role")
                                };
                            }
                        }
                    }
                }
                return user;
            }
            catch (Exception ex)
            {
                // Log the exception (use a logging framework like Serilog or NLog)
                Console.WriteLine($"Error in GetUser: {ex.Message}");
                return null;
            }
        }

        public bool AddUser(User user)
        {
            string query = "INSERT INTO Users (Username, Email, PasswordHash, Role) "
            + "VALUES (@username, @email, @passwordHash, @role);";

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", user.Username);
                        command.Parameters.AddWithValue("@email", user.Email);
                        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
                        command.Parameters.AddWithValue("@role", user.Role); 
                
                        connection.Open();
                        return command.ExecuteNonQuery() > 0; // Returns true if a row was inserted
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Log MySQL-specific exceptions (e.g., duplicate key errors)
                Console.WriteLine($"MySQL Error in AddUser: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log general exceptions
                Console.WriteLine($"Error in AddUser: {ex.Message}");
            }

            return false; // Return false if an exception occurred
        }
    }
}