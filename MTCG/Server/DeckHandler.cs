using System.Text;
using System.Text.Json.Nodes;
using MTCG.Models;

namespace MTCG.Server
{
    public class DeckHandler : Handler, IHandler
    {
        public override bool Handle(HttpSvrEventArgs e)
        {
            if (e.Path.TrimEnd('/', ' ', '\t') == "/cards" && e.Method == "GET")
            {
                return GetUserCards(e);
            }
            else if (e.Path.Split('?')[0].TrimEnd('/', ' ', '\t') == "/deck")
            {
                if (e.Method == "PUT")
                    return DefineDeck(e);
                if (e.Method == "GET")
                    return GetDeck(e);
            }
            return false;
        }

        private bool GetUserCards(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request" };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User is null)
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Unauthorized: Invalid token.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                string username = auth.User.UserName;
                DBHandler dbHandler = new();

                // Fetch cards for the user
                List<Card> userCards = dbHandler.GetUserCards(username);

                // Format the cards as JSON
                var cardsJson = new JsonArray();
                foreach (var card in userCards)
                {
                    cardsJson.Add(new JsonObject
                    {
                        ["id"] = card.Id,
                        ["name"] = card.Name,
                        ["damage"] = card.Damage,
                        ["element_type"] = (card is MonsterCard monsterCard) ? monsterCard.ElementType.ToString() : ((SpellCard)card).ElementType.ToString(),
                        ["type"] = card is MonsterCard ? "Monster" : "Spell"
                    });
                }

                status = HttpStatusCode.OK;
                reply = new JsonObject
                {
                    ["success"] = true,
                    ["cards"] = cardsJson
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user cards: {ex.Message}");
                status = HttpStatusCode.INTERNAL_SERVER_ERROR;
                reply["message"] = $"An internal server error occurred: {ex.Message}";
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }
        private bool DefineDeck(HttpSvrEventArgs e) 
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request" };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User is null)
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Unauthorized: Invalid token.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                string username = auth.User.UserName;

                JsonNode? payload = JsonNode.Parse(e.Payload);
                if (payload is null || !payload.AsArray().Any())
                {
                    status = HttpStatusCode.BAD_REQUEST;
                    reply["message"] = "Invalid payload.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                List<string> cardIds = payload.AsArray().Select(x => x.ToString()).ToList();
                if (cardIds.Count != 4)
                {
                    status = HttpStatusCode.BAD_REQUEST;
                    reply["message"] = "A deck must consist of exactly 4 cards.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                DBHandler dbHandler = new();

                // Check if the card belongs to user's stack
                foreach (var cardId in cardIds)
                {
                    if (!dbHandler.CardBelongsToUser(username, cardId))
                    {
                        status = HttpStatusCode.NOT_FOUND;
                        reply["message"] = $"Error: Card {cardId} does not belong to user {username} or does not exist.";
                        e.Reply(status, reply.ToJsonString());
                        return true;
                    }
                }

                // Update the deck with 4 cards
                dbHandler.ResetDeck(username);
                dbHandler.DefineDeck(username, cardIds);

                status = HttpStatusCode.OK;
                reply["success"] = true;
                reply["message"] = "Deck successfully updated.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error defining deck: {ex.Message}");
                status = HttpStatusCode.INTERNAL_SERVER_ERROR;
                reply["message"] = $"Error: {ex.Message}";
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }
        
        private bool GetDeck(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request" };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                Console.WriteLine($"Requested Path: {e.Path}");

                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User is null)
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Unauthorized: Invalid token.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                string username = auth.User.UserName;

                // Fetch the deck
                DBHandler dbHandler = new();
                List<Card> deck = dbHandler.GetDeck(username);
                
                string? queryString = e.Path.Contains('?') ? e.Path.Split('?')[1] : null;
                bool isPlainFormat = queryString != null && queryString.Contains("format=plain", StringComparison.OrdinalIgnoreCase);
                
                // for debugging purposes
                Console.WriteLine($"Query String: {queryString}");
                Console.WriteLine($"Is Plain Format: {isPlainFormat}");
                
                // Check if plain format is requested
                if (isPlainFormat)
                {
                    // Use StringWriter for plain text generation
                    using (var plainTextResponse = new StringWriter())
                    {
                        plainTextResponse.WriteLine("Your Deck:");
                        foreach (var card in deck)
                        {
                            string type = card is MonsterCard ? "Monster" : "Spell";
                            string elementType = card is MonsterCard monsterCard
                                ? monsterCard.ElementType.ToString()
                                : ((SpellCard)card).ElementType.ToString();

                            plainTextResponse.WriteLine($"- {card.Name} ({type}, {elementType}, Damage: {card.Damage})");
                        }
                        e.Reply(HttpStatusCode.OK, plainTextResponse.ToString());
                        return true;
                    }
                }

                // Return the deck - default format
                var cardsJson = new JsonArray();
                foreach (var card in deck)
                {
                    cardsJson.Add(new JsonObject
                    {
                        ["id"] = card.Id,
                        ["name"] = card.Name,
                        ["damage"] = card.Damage,
                        ["element_type"] = (card is MonsterCard monsterCard) ? monsterCard.ElementType.ToString() : ((SpellCard)card).ElementType.ToString(),
                        ["type"] = card is MonsterCard ? "Monster" : "Spell"
                    });
                }

                status = HttpStatusCode.OK;
                reply = new JsonObject
                {
                    ["success"] = true,
                    ["cards"] = cardsJson
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching deck: {ex.Message}");
                status = HttpStatusCode.INTERNAL_SERVER_ERROR;
                reply["message"] = $"An internal server error occurred: {ex.Message}";
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }
    }
}
