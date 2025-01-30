﻿using System.Text.Json.Nodes;
using MTCG.Server;

public class ScoreboardHandler : Handler, IHandler
{
    public override bool Handle(HttpSvrEventArgs e)
    {
        if (e.Path.TrimEnd('/', ' ', '\t') == "/scoreboard" && e.Method == "GET")
        {
            return GetScoreboard(e);
        }
        return false;
    }

    private bool GetScoreboard(HttpSvrEventArgs e)
    {
        JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request" };
        int status = HttpStatusCode.BAD_REQUEST;

        try
        {
            // Authenticate request
            (bool Success, User? User) auth = Token.Authenticate(e);
            if (!auth.Success || auth.User is null)
            {
                status = HttpStatusCode.UNAUTHORIZED;
                reply["message"] = "Unauthorized: Invalid token.";
                e.Reply(status, reply.ToJsonString());
                return true;
            }

            DBHandler dbHandler = new();
            List<User> scoreboard = dbHandler.GetScoreboard();

            JsonArray usersArray = new();
            foreach (var user in scoreboard)
            {
                usersArray.Add(new JsonObject
                {
                    ["username"] = user.UserName,
                    ["elo"] = user.elo,
                    ["games_played"] = user.games_played,
                    ["wins"] = user.wins,
                    ["losses"] = user.losses
                });
            }

            status = HttpStatusCode.OK;
            reply = new JsonObject
            {
                ["success"] = true,
                ["scoreboard"] = usersArray
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching scoreboard: {ex.Message}");
            status = HttpStatusCode.INTERNAL_SERVER_ERROR;
            reply["message"] = $"An internal server error occurred: {ex.Message}";
        }

        e.Reply(status, reply.ToJsonString());
        return true;
    }
}
