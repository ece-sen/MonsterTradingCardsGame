using System;
using System.Text.Json.Nodes;



namespace MTCG.Server
{
    public class SessionHandler: Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [override] Handler                                                                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Handles an incoming HTTP request.</summary>
        /// <param name="e">Event arguments.</param>
        public override bool Handle(HttpSvrEventArgs e)
        {
            if((e.Path.TrimEnd('/', ' ', '\t') == "/sessions") && (e.Method == "POST"))
            {                                                                   // POST /sessions will create a new session
                return _CreateSession(e);
            }

            return false;
        }



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Creates a session.</summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>Returns TRUE.</returns>
        public static bool _CreateSession(HttpSvrEventArgs e)
        {
            JsonObject? reply = new JsonObject() { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST; // Initialize response

            try
            {
                // Parse the request payload
                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json == null)
                {
                    reply["message"] = "Invalid JSON payload.";
                    e.Reply(status, reply?.ToJsonString());
                    return true;
                }

                // Validate required fields
                string? username = json["username"]?.ToString();
                string? password = json["password"]?.ToString();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    reply["message"] = "Username or password is missing.";
                    e.Reply(status, reply?.ToJsonString());
                    return true;
                }

                // Attempt logon
                var result = User.Logon(username, password);
                if (result.Success)
                {
                    // Logon was successful
                    status = HttpStatusCode.OK;
                    reply = new JsonObject()
                    {
                        ["success"] = true,
                        ["message"] = $"{username} authenticated successfully.",
                        ["token"] = result.Token
                    };
                }
                else
                {
                    // Logon failed
                    status = HttpStatusCode.UNAUTHORIZED;
                    reply["message"] = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions and log them for debugging
                reply["message"] = $"Unexpected error: {ex.Message}";
            }

            // Send the reply
            e.Reply(status, reply?.ToJsonString());
            return true;
        }

    }
}