using Npgsql;

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
        private static readonly object _dbLock = new();
        public void CreateUser(string userName, string password, int coins, int elo)
        {
            lock (_dbLock)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();
                        var query = "INSERT INTO users (username, password, coins, elo) VALUES (@username, @password, @coins, @elo)";
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("username", userName);
                            cmd.Parameters.AddWithValue("password", password);
                            cmd.Parameters.AddWithValue("coins", coins);
                            cmd.Parameters.AddWithValue("elo", elo);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (PostgresException ex) when (ex.SqlState == "23505")
                {
                    throw new Exception("User name already exists.");
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while interacting with the database.", ex);
                }
            }
            
        }


        /// <summary>
        /// Get user by username.
        /// </summary>
        public User? GetUser(string userName)
        {
            lock (_dbLock)
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
            }
            
            return null;
        }

        /// <summary>
        /// Update user information.
        /// </summary>
        public void UpdateUser(string userName, string password, int coins, int elo)
        {
            lock (_dbLock)
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

        public void AddCardToPackage(string? id, string? name, double damage)
        {
            lock (_dbLock)
            {
                const string query = @"
                    INSERT INTO cards (id, name, damage) 
                    VALUES (@id, @name, @damage)";
                using var connection = GetConnection();
                connection.Open();
                using var command = new NpgsqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@damage", damage);
                
                command.ExecuteNonQuery();
            }
        }
    }
}
