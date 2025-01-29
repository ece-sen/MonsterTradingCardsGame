using System.Text.Json.Nodes;
using System;
using MTCG.Models;

namespace MTCG.Server
{
    public class CardHandler : Handler, IHandler
    {
        public override bool Handle(HttpSvrEventArgs e)
        {
            if (e.Path.TrimEnd('/', ' ', '\t') == "/packages" && e.Method == "POST")
            {
                return AddPackageWithCards(e);
            }
            return false;
        }

        private bool AddPackageWithCards(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request" };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                // Authenticate request
                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User is null || auth.User.UserName != "admin")
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Unauthorized: Only admin can add packages.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                // Parse payload
                JsonNode? payload = JsonNode.Parse(e.Payload);
                if (payload is null || !payload.AsArray().Any())
                {
                    status = HttpStatusCode.BAD_REQUEST;
                    reply["message"] = "Invalid payload.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                string packageId = Guid.NewGuid().ToString();
                var dbHandler = new DBHandler();

                foreach (JsonNode cardNode in payload.AsArray())
                {
                    if (cardNode is null) continue;

                    string id = (string)cardNode["Id"];
                    string name = (string)cardNode["Name"];
                    double damage = (double)cardNode["Damage"];

                    // Determine if the card is a SpellCard or MonsterCard
                    Card card = name.Contains("Spell", StringComparison.OrdinalIgnoreCase)
                        ? new SpellCard(id, name, damage)
                        : new MonsterCard(id, name, damage);

                    // Insert card into the database
                    string elementType = card is MonsterCard monsterCard
                        ? monsterCard.ElementType.ToString()
                        : ((SpellCard)card).ElementType.ToString();

                    string type = card is SpellCard ? "Spell" : "Monster";
                    dbHandler.AddCard(id, name, damage, elementType, type);

                    // Link card to the package
                    dbHandler.AddCardToPackage(packageId, id);
                }

                status = HttpStatusCode.CREATED;
                reply["success"] = true;
                reply["message"] = "Package and cards added successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding package: {ex.Message}\n{ex.StackTrace}");
                status = HttpStatusCode.INTERNAL_SERVER_ERROR;
                reply["message"] = $"An internal server error occurred: {ex.Message}";
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }
    }
}
