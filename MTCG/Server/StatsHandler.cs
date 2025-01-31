using System.Text.Json.Nodes;
using MTCG.Server;

public class StatsHandler : Handler, IHandler
{
    public override bool Handle(HttpSvrEventArgs e)
    {
        if (e.Path.TrimEnd('/', ' ', '\t') == "/stats" && e.Method == "GET")
        {
            return GetUserStats(e);
        }
        return false;
    }

    private bool GetUserStats(HttpSvrEventArgs e)
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

            // Fetch user stats from database
            User? user = dbHandler.GetUser(username);
            if (user == null)
            {
                status = HttpStatusCode.NOT_FOUND;
                reply["message"] = "User not found.";
                e.Reply(status, reply.ToJsonString());
                return true;
            }

            reply = new JsonObject
            {
                ["success"] = true,
                ["username"] = user.UserName,
                ["elo"] = user.Elo,
                ["coins"] = user.Coins
            };

            status = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching user stats: {ex.Message}");
            status = HttpStatusCode.INTERNAL_SERVER_ERROR;
            reply["message"] = $"An internal server error occurred: {ex.Message}";
        }

        e.Reply(status, reply.ToJsonString());
        return true;
    }
}
