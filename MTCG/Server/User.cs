﻿using System;
using System.Security;
using System.Security.Authentication;

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

        /// <summary>Gets the username.</summary>
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
        
        public int Coins { get; set; }
        public int Elo { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string Bio { get; set; } = string.Empty;  
        public string Image { get; set; } = string.Empty; 
        public int GamesPlayed { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        
        /// <summary>Creates a user.</summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="fullName">Full name.</param>
        public static void Create(string userName, string password)
        {
            User user = new()
            {
                UserName = userName,
                Password = password,
                Coins = 20,
                Elo = 100,
                Name = "",
                Bio = "",
                Image = ""
          
            }; 
            DBHandler dbHandler = new DBHandler();
            dbHandler.CreateUser(userName, password, 20, 100, "", "", "");
        }

        /// <summary>Gets a user by username.</summary>
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