using System.Data;
using Npgsql;
using MTCG.Models;

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
        public void CreateUser(string userName, string password, int coins, int elo, string bio, string image)
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
                const string query = "SELECT username, password, coins, elo, bio, image FROM users WHERE username = @username;";

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
                        Bio = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Image = reader.IsDBNull(5) ? "" : reader.GetString(5)
                    };
                }
            }
            
            return null;
        }

        /// <summary>
        /// Update user information with optional fields.
        /// </summary>
        public void UpdateUser(string oldUserName, string? newUserName = null, string? password = null, int? coins = null, int? elo = null, string? bio = null, string? image = null)
        {
            lock (_dbLock)
            {
                using var connection = GetConnection();
                connection.Open();
                

                // Fetch current values first
                const string selectQuery = "SELECT username, password, coins, elo, bio, image FROM users WHERE username = @username;";
                string currentUserName = oldUserName;
                string currentPassword = "";
                int currentCoins = 0, currentElo = 0;
                string currentBio = "", currentImage = "";

            using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
            {
                selectCommand.Parameters.AddWithValue("@username", oldUserName);
                using var reader = selectCommand.ExecuteReader();
                if (reader.Read())
                {
                    currentUserName = reader.GetString(0);
                    currentPassword = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    currentCoins = reader.GetInt32(2);
                    currentElo = reader.GetInt32(3);
                    currentBio = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    currentImage = reader.IsDBNull(5) ? "" : reader.GetString(5);
                }
                else
                {
                    throw new Exception($"User '{oldUserName}' not found.");
                }
            }

            // Use existing values if new ones are not provided
            newUserName??= currentUserName;
            password ??= currentPassword;
            coins ??= currentCoins;
            elo ??= currentElo;
            bio ??= currentBio;
            image ??= currentImage;

            // If username is being changed, update it separately
            if (!oldUserName.Equals(newUserName, StringComparison.OrdinalIgnoreCase))
            {
                const string updateUsernameQuery = "UPDATE users SET username = @newUsername WHERE username = @oldUsername;";
                using var updateUsernameCommand = new NpgsqlCommand(updateUsernameQuery, connection);
                updateUsernameCommand.Parameters.AddWithValue("@newUsername", newUserName);
                updateUsernameCommand.Parameters.AddWithValue("@oldUsername", oldUserName);
                int rowsAffected = updateUsernameCommand.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new Exception($"Failed to update username from '{oldUserName}' to '{newUserName}'.");
                }
            }

            // Update the remaining user details
            const string updateQuery = @"
            UPDATE users 
            SET password = @password, coins = @coins, elo = @elo, bio = @bio, image = @image 
            WHERE username = @username;";

            using var updateCommand = new NpgsqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@username", newUserName);
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
        public void DefineDeck(string username, List<string> cardIds)
        {
            lock (_dbLock)
            {
                const string resetDeckQuery = @"
            UPDATE cards
            SET in_deck = FALSE
            WHERE owner = @username;";

                const string setDeckQuery = @"
            UPDATE cards
            SET in_deck = TRUE
            WHERE id = @card_id AND owner = @username;";

                using var connection = GetConnection();
                connection.Open();

                // Reset all cards in the user's collection
                using (var resetCommand = new NpgsqlCommand(resetDeckQuery, connection))
                {
                    resetCommand.Parameters.AddWithValue("@username", username);
                    resetCommand.ExecuteNonQuery();
                }

                // Set the new deck
                foreach (var cardId in cardIds)
                {
                    using var setDeckCommand = new NpgsqlCommand(setDeckQuery, connection);
                    setDeckCommand.Parameters.AddWithValue("@card_id", Guid.Parse(cardId));
                    setDeckCommand.Parameters.AddWithValue("@username", username);

                    int rowsAffected = setDeckCommand.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Card {cardId} does not belong to user {username} or does not exist.");
                    }
                }
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
    }
}
