using System;
using System.Text.Json.Nodes;
namespace MTCG.Server
{
    public class UserHandler : Handler, IHandler
    {
        public override bool Handle(HttpSvrEventArgs e)
        {
            if ((e.Path.TrimEnd('/', ' ', '\t') == "/users") && (e.Method == "POST"))
            { // POST /users will create a user object
                return _CreateUser(e);
            }
            else if (e.Path.StartsWith("/users/") && (e.Method == "GET"))
            { // GET /users/UserName will query a user
                return _QueryUser(e);
            }
            else if (e.Path.StartsWith("/users/") && (e.Method == "PUT"))
            { // PUT /users/UserName will update a user
                return _UpdateUser(e);
            }
            else if ((e.Path.TrimEnd('/', ' ', '\t') == "/sessions") && (e.Method == "POST"))
            {
                return _Logon(e);
            }

            return false;
        }

        private static bool _CreateUser(HttpSvrEventArgs e)
        {
            JsonObject? reply = new JsonObject { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json != null)
                {
                    string username = (string)json["Username"]!;
                    string password = (string)json["Password"]!;

                    User.Create(username, password);
                    status = HttpStatusCode.CREATED; // HTTP 201
                    reply = new JsonObject { ["success"] = true, ["message"] = "User created." };
                }
            }
            catch (Exception ex)
            {
                // Check the exception message to customize the status code
                if (ex.Message == "User name already exists.")
                {
                    status = HttpStatusCode.CONFLICT; // HTTP 409
                }
                else
                {
                    status = HttpStatusCode.INTERNAL_SERVER_ERROR; // HTTP 500
                }

                reply = new JsonObject { ["success"] = false, ["message"] = ex.Message };
            }

            e.Reply(status, reply?.ToJsonString());
            return true;
        }


        private static bool _QueryUser(HttpSvrEventArgs e)
        {
            JsonObject? reply = new JsonObject { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                // Authenticate the request
                (bool Success, User? User) ses = Token.Authenticate(e);
                Console.WriteLine($"Token Authentication Success: {ses.Success}");

                if (!ses.Success)
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Unauthorized.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                // Parse the username from the URL
                string userName = e.Path[7..];
                Console.WriteLine($"Fetching details for user: {userName}");
                
                // ✅ Step 3: Ensure the authenticated user is requesting their own data
                if (!ses.User.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "You are not allowed to view this user's information.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }


                // Retrieve user from the database
                DBHandler dbHandler = new DBHandler();
                User? user = dbHandler.GetUser(userName);

                if (user == null)
                {
                    status = HttpStatusCode.NOT_FOUND;
                    reply["message"] = "User not found.";
                }
                else
                {
                    status = HttpStatusCode.OK;
                    reply = new JsonObject
                    {
                        ["success"] = true,
                        ["username"] = user.UserName,
                        ["name"] = user.Name,
                        ["coins"] = user.coins,
                        ["elo"] = user.elo,
                        ["Bio"] = user.Bio,
                        ["Image"] = user.Image,
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in _QueryUser: {ex.Message}");
                reply["message"] = ex.Message;
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }


        /// <summary>
        /// Updates a user.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>Returns TRUE.</returns>
        private bool _UpdateUser(HttpSvrEventArgs e)
        {
            JsonObject? reply = new JsonObject { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                (bool Success, User? User) ses = Token.Authenticate(e);
                if (!ses.Success)
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Unauthorized.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }
                
                string userName = e.Path[7..];
                
                // Ensure users can only update their own profile
                if (!ses.User.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "You can only update your own profile.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json == null)
                {
                    status = HttpStatusCode.BAD_REQUEST;
                    reply["message"] = "Invalid JSON payload.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }
                
                string? newName = json["Name"]?.GetValue<string>();
                string? newPassword = json["Password"]?.GetValue<string>();
                int? coins = json["Coins"]?.GetValue<int?>();
                int? elo = json["Elo"]?.GetValue<int?>();
                string? bio = json["Bio"]?.GetValue<string>();
                string? image = json["Image"]?.GetValue<string>();

                DBHandler dbHandler = new DBHandler();
                dbHandler.UpdateUser(userName, newName, newPassword, coins, elo, bio, image); // Username remains the same

                status = HttpStatusCode.OK;
                reply["success"] = true;
                reply["message"] = "User updated successfully.";
            }
            catch (Exception ex)
            {
                reply["message"] = ex.Message;
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }


        
        private static bool _Logon(HttpSvrEventArgs e)
        {
            JsonObject? reply = new JsonObject { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                // Parse the login credentials from the request payload
                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json == null || json["Username"] == null || json["Password"] == null)
                {
                    reply["message"] = "Missing username or password.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                string username = (string)json["Username"]!;
                string password = (string)json["Password"]!;

                // Validate credentials
                DBHandler dbHandler = new DBHandler();
                User? user = dbHandler.GetUser(username);
                if (user == null || user.Password != password)
                {
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Invalid username or password.";
                    e.Reply(status, reply.ToJsonString());
                    return true;
                }

                // Generate a token for the user
                string token = Token._CreateTokenFor(user);
                status = HttpStatusCode.OK;
                reply = new JsonObject
                {
                    ["success"] = true,
                    ["message"] = "Login successful.",
                    ["token"] = token
                };
            }
            catch (Exception ex)
            {
                reply["message"] = ex.Message;
            }

            e.Reply(status, reply.ToJsonString());
            return true;
        }

    }
}
