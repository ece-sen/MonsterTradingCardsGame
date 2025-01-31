using System.Text.Json.Nodes;
using MTCG.Server;

public class TransactionHandler : Handler, IHandler
{
    public override bool Handle(HttpSvrEventArgs e)
    {
        if (e.Path.TrimEnd('/', ' ', '\t') == "/transactions/packages" && e.Method == "POST")
        {
            return PurchasePackage(e);
        }
        return false;
    }

    private bool PurchasePackage(HttpSvrEventArgs e)
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

            string username = auth.User.UserName;
            DBHandler dbHandler = new();

            // Check if there are available packages BEFORE deducting coins
            string packageId = dbHandler.GetAvailablePackage();
            if (string.IsNullOrEmpty(packageId))
            {
                status = HttpStatusCode.NOT_FOUND;
                reply["message"] = "No packages available.";
                e.Reply(status, reply.ToJsonString());
                return true; 
            }
            // Deduct 5 coins ONLY if package exists
            dbHandler.DeductCoins(username, 5);

            // Assign the package to the user
            dbHandler.AssignPackageToUser(username, packageId);

            status = HttpStatusCode.CREATED;
            reply["success"] = true;
            reply["message"] = "Package purchased successfully.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error purchasing package: {ex.Message}");
            status = HttpStatusCode.INTERNAL_SERVER_ERROR;
            reply["message"] = $"An internal server error occurred: {ex.Message}";
        }

        e.Reply(status, reply.ToJsonString());
        return true;
    }
}
