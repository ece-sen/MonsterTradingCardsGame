using System.Text.Json.Nodes;
using System;

namespace MTCG.Server;

public class PackageHandler : Handler, IHandler
{
    public override bool Handle(HttpSvrEventArgs e)
    {
        if (e.Path.TrimEnd('/', ' ', '\t') == "/packages" && e.Method == "POST")
        {
            return _CreatePackage(e);
        }
        return false;
    }

    private bool _CreatePackage(HttpSvrEventArgs e)
    {
        JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request" };
        int status = HttpStatusCode.BAD_REQUEST;

        try
        {
            //Authenticate request
            (bool Success, User? User)? auth = Token.Authenticate(e);
            if (!auth.Success || auth.User is null || auth.User.UserName != "admin")
            {
                status = HttpStatusCode.UNAUTHORIZED;
                reply["message"] = "Unauthorized: only admin can create packages";
                e.Reply(status, reply.ToJsonString());
                return true;
            }
            
            //Parse and validate the payload
            JsonNode? payload = JsonNode.Parse(e.Payload);
            if (payload is null || !payload.AsArray().Any())
            {
                status = HttpStatusCode.BAD_REQUEST;
                reply["message"] = "Invalid payload";
                e.Reply(status, reply.ToJsonString());
                return true;
            }
            
            //Create package
            DBHandler dbHandler = new();
            foreach (JsonNode cardNode in payload.AsArray())
            {
                if(cardNode is null) continue;
                string id = (string)cardNode["Id"];
                string name = (string)cardNode["Name"];
                double damage = (double)cardNode["Damage"];

                dbHandler.AddCardToPackage(id, name, damage);
            }
            
            status =HttpStatusCode.CREATED;
            reply["success"] = true;
            reply["message"] = "Package created";
        }
        catch (Exception ex)
        { 
            Console.WriteLine($"Error creating package: {ex.Message}");
            status = HttpStatusCode.INTERNAL_SERVER_ERROR;
            reply["message"] = "An internal server error occured";
        }
        
        e.Reply(status, reply.ToJsonString());
        return true;
    }
}