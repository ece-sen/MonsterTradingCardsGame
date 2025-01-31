using System;
using System.Collections.Concurrent;

namespace MTCG.Server
{
    /// <summary>This class provides methods for the token-based security.</summary>
    public static class Token
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private constants                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Alphabet string.</summary>
        private const string _ALPHABET = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static members                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Token dictionary.</summary>
        internal static ConcurrentDictionary<string, User> _Tokens = new();
        
        /// <summary>Username-to-token mapping for managing single token per user.</summary>
        private static ConcurrentDictionary<string, string> _UserTokens = new();


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static methods                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Creates a new token for a user.</summary>
        /// <param name="user">User.</param>
        /// <returns>Token string.</returns>
        public static string _CreateTokenFor(User user)
        {
            string token = $"{user.UserName}-mtcgToken"; // Token-Format: username-mtcgToken
            _Tokens[token] = user;
            return token;
        }
        /*
        public static string _CreateTokenFor(User user)
        {
            string token;
            Random rnd = new();
            
            // Invalidate the existing token for this user, if any
            InvalidateTokenForUser(user.UserName);

            do
            {
                // Generate a random 24-character token
                token = string.Concat(Enumerable.Range(0, 24).Select(_ => _ALPHABET[rnd.Next(_ALPHABET.Length)]));
            }
            while (!_Tokens.TryAdd(token, user)); // Ensure the token is unique and successfully added
            
            // Map the new token to the username
            _UserTokens[user.UserName] = token;
            
            return token;
        }
*/
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Authenticates a user by token.</summary>
        /// <param name="token">Token string.</param>
        /// <returns>Returns a tuple of success flag and user object.
        ///          If successful, the success flag is TRUE and the user represents the authenticated user,
        ///          otherwise success flag is FALSE and user object is NULL.</returns>
        public static (bool Success, User? User) Authenticate(string token)
        {
            if (Program.ALLOW_DEBUG_TOKEN && token.EndsWith("-debug"))
            {                                                                   // Accept debug token
                token = token[..^6];
                User? user1 = User.Get(token);
                return (user1 != null, user1);
            }

            // Use TryGetValue to safely fetch the user associated with the token
            if (_Tokens.TryGetValue(token, out User? user))
            {
                return (true, user);
            }

            return (false, null);
        }

        /// <summary>Authenticates a user by token.</summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>Returns a tuple of success flag and user object.
        ///          If successful, the success flag is TRUE and the user represents the authenticated user,
        ///          otherwise success flag is FALSE and user object is NULL.</returns>
        public static (bool Success, User? User) Authenticate(HttpSvrEventArgs e)
        {
            foreach (HttpHeader i in e.Headers)
            {                                                                   // Iterate headers
                if (i.Name == "Authorization")
                {                                                               // Found "Authorization" header
                    if (i.Value.StartsWith("Bearer "))
                    {                                                           // Needs to start with "Bearer "
                        return Authenticate(i.Value[7..].Trim());               // Authenticate by token
                    }
                    break;
                }
            }

            return (false, null);                                               // "Authorization" header not found, authentication failed
        }

        /// <summary>Removes a token from the dictionary.</summary>
        /// <param name="token">Token string.</param>
        /// <returns>True if the token was removed successfully, otherwise false.</returns>
        public static bool RemoveToken(string token)
        {
            if (_Tokens.TryRemove(token, out var user))
            {
                if (user != null)
                {
                    _UserTokens.TryRemove(user.UserName, out _);
                }
                return true;
            }
            return false;
        }

        /// <summary>Invalidates the current token for the specified user.</summary>
        /// <param name="username">Username.</param>
        private static void InvalidateTokenForUser(string username)
        {
            // Check if a token exists for the user
            if (_UserTokens.TryGetValue(username, out var existingToken))
            {
                // Remove the token from the dictionaries
                RemoveToken(existingToken);
            }
        }

        /// <summary>Invalidates all tokens (useful for cleanup or session termination).</summary>
        public static void InvalidateAllTokens()
        {
            _Tokens.Clear();
            _UserTokens.Clear();
        }
    }
}
