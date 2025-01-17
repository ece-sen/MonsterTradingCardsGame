﻿using Npgsql;

namespace MTCG.Server
{
    public class DBHandler
    {
        private readonly string _connectionString;

        public DBHandler()
        {
            _connectionString = "Host=localhost;Username=swen1;Password=swen1;Database=swen1";
        }

        // Open a connection to the database
        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        /// <summary>
        /// Create a new user in the database.
        /// </summary>
        public void CreateUser(string userName, string password, int coins, int elo)
        {
            try
            {
                const string query = @"
                INSERT INTO users (username, password, coins, elo) 
                VALUES (@username, @password, @coins, @elo);";

                
                using var connection = GetConnection();
                connection.Open();
                using var command = new NpgsqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@username", userName);
                command.Parameters.AddWithValue("@password", password);
                command.Parameters.AddWithValue("@coins", coins);
                command.Parameters.AddWithValue("@elo", elo);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        
            

        }

        /// <summary>
        /// Get user by username.
        /// </summary>
        public User? GetUser(string userName)
        {
            const string query = "SELECT username, password, coins, elo FROM users WHERE username = @username;";

            using var connection = GetConnection();
            connection.Open();
            using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@username", userName);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserName = reader.GetString(0),
                    Password = reader.GetString(1),
                    coins = reader.GetInt32(2),
                    elo = reader.GetInt32(3)
                };
            }

            return null;
        }

        /// <summary>
        /// Update user information.
        /// </summary>
        public void UpdateUser(string userName, string password, int coins, int elo)
        {
            const string query = @"
                UPDATE users 
                SET password = @password, coins = @coins, elo = @elo 
                WHERE username = @username;";

            using var connection = GetConnection();
            connection.Open();
            using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@username", userName);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@coins", coins);
            command.Parameters.AddWithValue("@elo", elo);

            command.ExecuteNonQuery();
            
            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new Exception($"User '{userName}' not found.");
            }
        }
    }
}