using System;
using System.Security;
using System.Security.Authentication;

using MTCG.Exceptions;



namespace MTCG.Server
{
    /// <summary>This class represents a user.</summary>
    public sealed class User
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static members                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Currently holds the system users.</summary>
        /// <remarks>Is to be removed by database implementation later.</remarks>
        private static Dictionary<string, User> _Users = new();



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructors                                                                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Creates a new instance of this class.</summary>
        public User()
        {}



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets the user name.</summary>
        public string UserName
        {
            get;
            set;
        } = string.Empty;




        /// <summary>Gets or sets the user's password.</summary>
        public string Password
        {
            get;
            set;
        } = string.Empty;
        
        public int coins { get; set; }
        
        public int elo { get; set; }
        

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Creates a user.</summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="fullName">Full name.</param>
        /// <param name="eMail">E-mail addresss.</param>
        public static void Create(string userName, string password)
        {
            User user = new()
            {
                UserName = userName,
                Password = password,
                coins = 20,
                elo = 100
          
            }; 
            DBHandler dbHandler = new DBHandler();
            dbHandler.CreateUser(userName, password, 20, 100);
        }
        
        public void Save(string token)
        {
            (bool Success, User? User) auth = Token.Authenticate(token);
            if (auth.Success)
            {
                if (auth.User!.UserName != UserName)
                {
                    throw new SecurityException("Trying to change other user's data.");
                }

                // Call the DBHandler to persist changes
                DBHandler dbHandler = new DBHandler();
                dbHandler.UpdateUser(UserName, Password, coins, elo);
            }
            else
            {
                throw new AuthenticationException("Not authenticated.");
            }
        }


        /// <summary>Gets a user by user name.</summary>
        /// <param name="userName">User name.</param>
        /// <returns>Return a user object if the user was found, otherwise returns NULL.</returns>
        public static User? Get(string userName) 
        {
            _Users.TryGetValue(userName, out User? user);
            return user;
        }


        /// <summary>Performs a user logon.</summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns a tuple of success flag and token.
        ///          If successful, the success flag is TRUE and the token contains a token string,
        ///          otherwise success flag is FALSE and token is empty.</returns>
        public static (bool Success, string Token) Logon(string userName, string password)
        {
            // Retrieve the user from the database
            DBHandler dbHandler = new DBHandler();
            User? user = dbHandler.GetUser(userName);

            if (user != null)
            {
                // Verify the password
                if (user.Password == password)
                {
                    // Create and return a token for the user
                    return (true, Token._CreateTokenFor(user));
                }

                // Password mismatch
                return (false, string.Empty);
            }

            // User not found
            return (false, string.Empty);
        }

    }
}