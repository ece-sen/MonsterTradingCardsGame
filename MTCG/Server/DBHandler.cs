using System.Data;
using Npgsql;
using MTCG.Models;

namespace MTCG.Server
{
    public class DBHandler
    {
        private readonly string _connectionString;

        public DBHandler(bool useTestDb = false)
        {
            if (useTestDb)  // for unit tests
            {
                _connectionString = "Host=localhost;Username=swen1;Password=swen1;Database=swen1_test";
            }
            else
            {
                _connectionString = "Host=localhost;Username=swen1;Password=swen1;Database=swen1";
            }
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
        public void CreateUser(string userName, string password, int coins, int elo, string Name, string bio, string image)
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
                            cmd.Parameters.AddWithValue("Name", Name?? "");
                            cmd.Parameters.AddWithValue("bio", bio?? "");
                            cmd.Parameters.AddWithValue("image", image?? "");

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
                const string query = "SELECT username, password, coins, elo, name, bio, image FROM users WHERE username = @username;";

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
                        elo = reader.GetInt32(3),
                        Name = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Bio = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Image = reader.IsDBNull(6) ? "" : reader.GetString(6)
                    };
                }
            }
            
            return null;
        }

        /// <summary>
        /// Update user information with optional fields.
        /// </summary>
        public void UpdateUser(string username, string? newName = null, string? password = null, int? coins = null, int? elo = null, string? bio = null, string? image = null)
        {
            lock (_dbLock)
            {
                using var connection = GetConnection();
                connection.Open();


                // Fetch current values first
                const string selectQuery =
                    "SELECT name, password, coins, elo, bio, image FROM users WHERE username = @username;";
                string currentName = "";
                string currentPassword = "";
                int currentCoins = 0, currentElo = 0;
                string currentBio = "", currentImage = "";

                using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
                {
                    selectCommand.Parameters.AddWithValue("@username", username);
                    using var reader = selectCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        currentName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        currentPassword = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        currentCoins = reader.GetInt32(2);
                        currentElo = reader.GetInt32(3);
                        currentBio = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        currentImage = reader.IsDBNull(5) ? "" : reader.GetString(5);
                    }
                    else
                    {
                        throw new Exception($"User '{username}' not found.");
                    }
                }

                // Use existing values if new ones are not provided
                newName ??= currentName;
                password ??= currentPassword;
                coins ??= currentCoins;
                elo ??= currentElo;
                bio ??= currentBio;
                image ??= currentImage;

                // Update all user attributes except Username
                const string updateQuery = @"
                UPDATE users 
                SET name = @name, password = @password, coins = @coins, elo = @elo, bio = @bio, image = @image 
                WHERE username = @username;";

                using var updateCommand = new NpgsqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@username", username); // Username remains unchanged
                updateCommand.Parameters.AddWithValue("@name", newName);
                updateCommand.Parameters.AddWithValue("@password", password);
                updateCommand.Parameters.AddWithValue("@coins", coins);
                updateCommand.Parameters.AddWithValue("@elo", elo);
                updateCommand.Parameters.AddWithValue("@bio", bio);
                updateCommand.Parameters.AddWithValue("@image", image);

                updateCommand.ExecuteNonQuery();
            }
        }


        public void AddCard(string cardId, string name, double damage, string elementType, string type)
        {
            lock (_dbLock)
            {
                const string query = @"
                    INSERT INTO cards (id, name, damage, element_type, type)
                    VALUES (@id, @name, @damage, @element_type, @type)
                    ON CONFLICT (id) DO NOTHING;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", Guid.Parse(cardId));
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@damage", damage);
                command.Parameters.AddWithValue("@element_type", elementType);
                command.Parameters.AddWithValue("@type", type);

                command.ExecuteNonQuery();
            }
        }

        public void AddCardToPackage(string packageId, string cardId)
        {
            lock (_dbLock)
            {
                const string query = @"
            INSERT INTO packages (package_id, card_id)
            VALUES (@package_id, @card_id)
            ON CONFLICT DO NOTHING;"; // Avoid inserting duplicates

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@package_id", Guid.Parse(packageId));
                command.Parameters.AddWithValue("@card_id", Guid.Parse(cardId));

                command.ExecuteNonQuery();
            }
        }
        
        public void DeductCoins(string username, int coins)
        {
            lock (_dbLock)
            {
                const string query = @"
            UPDATE users 
            SET coins = coins - @coins 
            WHERE username = @username AND coins >= @coins;
            ";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@coins", coins);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new Exception("Insufficient coins or user not found.");
                }
            }
        }

        public void AssignPackageToUser(string username, string packageId)
        {
            lock (_dbLock)
            {
                const string query = @"
            UPDATE cards 
            SET owner = @username 
            WHERE id IN (SELECT card_id FROM packages WHERE package_id = @package_id)
            AND owner IS NULL;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@package_id", Guid.Parse(packageId));

                command.ExecuteNonQuery();
            }
        }
        public string GetAvailablePackage()
        {
            lock (_dbLock)
            {
                const string query = @"
            SELECT package_id 
            FROM packages 
            WHERE package_id IN (
                SELECT DISTINCT package_id 
                FROM packages p 
                JOIN cards c ON p.card_id = c.id 
                WHERE c.owner IS NULL
            )
            LIMIT 1;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                var result = command.ExecuteScalar();

                return result?.ToString() ?? string.Empty;
            }
        }
        public List<Card> GetUserCards(string username)
        {
            lock (_dbLock)
            {
                const string query = @"
            SELECT id, name, damage, element_type, type
            FROM cards
            WHERE owner = @username;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);

                using var reader = command.ExecuteReader();
                List<Card> cards = new();

                while (reader.Read())
                {
                    string id = reader.GetGuid(0).ToString();
                    string name = reader.GetString(1);
                    double damage = reader.GetDouble(2);
                    string elementType = reader.GetString(3);
                    string type = reader.GetString(4);

                    // Dynamically determine card type
                    if (type == "Monster")
                    {
                        cards.Add(new MonsterCard(id, name, damage));
                    }
                    else if (type == "Spell")
                    {
                        cards.Add(new SpellCard(id, name, damage));
                    }
                }

                return cards;
            }
        }
        public List<Card> GetDeck(string username)
        {
            lock (_dbLock)
            {
                const string query = @"
            SELECT id, name, damage, element_type, type
            FROM cards
            WHERE owner = @username AND in_deck = TRUE;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);

                using var reader = command.ExecuteReader();
                List<Card> deck = new();

                while (reader.Read())
                {
                    string id = reader.GetGuid(0).ToString();
                    string name = reader.GetString(1);
                    double damage = reader.GetDouble(2);
                    string elementType = reader.GetString(3);
                    string type = reader.GetString(4);

                    if (type == "Monster")
                    {
                        deck.Add(new MonsterCard(id, name, damage));
                    }
                    else if (type == "Spell")
                    {
                        deck.Add(new SpellCard(id, name, damage));
                    }
                }

                return deck;
            }
        }
        
        public bool CardBelongsToUser(string username, string cardId)
        {
            lock (_dbLock)
            {
                const string query = @"
        SELECT COUNT(*) FROM cards 
        WHERE id = @cardId AND owner = @username;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@cardId", Guid.Parse(cardId));
                command.Parameters.AddWithValue("@username", username);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0; // ✅ True if card exists & belongs to user
            }
        }
        
        public void DefineDeck(string username, List<string> cardIds)
        {
            lock (_dbLock)
            {
                const string query = @"
        UPDATE cards 
        SET in_deck = TRUE 
        WHERE id = @cardId AND owner = @username;";

                using var connection = GetConnection();
                connection.Open();

                foreach (var cardId in cardIds)
                {
                    using var command = new NpgsqlCommand(query, connection);
                    command.Parameters.AddWithValue("@cardId", Guid.Parse(cardId));
                    command.Parameters.AddWithValue("@username", username);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Card {cardId} does not belong to user {username} or does not exist.");
                    }
                }
            }
        }
        
        public void ResetDeck(string username)
        {
            lock (_dbLock)
            {
                const string query = @"
        UPDATE cards 
        SET in_deck = FALSE 
        WHERE owner = @username;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.ExecuteNonQuery();
            }
        }
        public void UpdateGameStats(string username, bool won, bool lost)
        {
            lock (_dbLock)
            {
                using var connection = GetConnection();
                connection.Open();

                const string query = @"
            UPDATE users 
            SET games_played = games_played + 1,
                wins = wins + CASE WHEN @won THEN 1 ELSE 0 END,
                losses = losses + CASE WHEN @lost THEN 1 ELSE 0 END
            WHERE username = @username;";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@won", won);
                command.Parameters.AddWithValue("@lost", lost);

                command.ExecuteNonQuery();
            }
        }
        public List<User> GetScoreboard()
        {
            lock (_dbLock)
            {
                const string query = @"
            SELECT username, elo, games_played, wins, losses 
            FROM users 
            ORDER BY elo DESC, wins DESC, games_played ASC;";

                using var connection = GetConnection();
                connection.Open();

                using var command = new NpgsqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                List<User> scoreboard = new();
                while (reader.Read())
                {
                    scoreboard.Add(new User
                    {
                        UserName = reader.GetString(0),
                        elo = reader.GetInt32(1),
                        games_played = reader.GetInt32(2),
                        wins = reader.GetInt32(3),
                        losses = reader.GetInt32(4)
                    });
                }

                return scoreboard;
            }
        }

    }
}
