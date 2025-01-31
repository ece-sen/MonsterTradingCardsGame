using NUnit.Framework;
using MTCG.Server;
using MTCG.Models;
using System;
using System.Collections.Generic;

namespace MTCG.Tests
{
    [TestFixture]
    public class ScoreboardTests
    {
        private DBHandler _dbHandler;

        [SetUp]
        public void Setup()
        {
            _dbHandler = new DBHandler(useTestDb: true);
        }

        private void EnsureUserExists(string username, int elo)
        {
            var user = _dbHandler.GetUser(username);
            if (user == null)
            {
                _dbHandler.CreateUser(username, "password", 20, elo, "", "", "");
            }
        }

        [Test]
        public void Scoreboard_ShouldOrderUsersByElo()
        {
            string user1 = "user1_test";
            string user2 = "user2_test";

            EnsureUserExists(user1, 500); 
            EnsureUserExists(user2, 400); 

            var scoreboard = _dbHandler.GetScoreboard();

            Assert.AreEqual(user1, scoreboard[0].UserName, "User with higher Elo should be ranked first.");
            Assert.AreEqual(user2, scoreboard[1].UserName, "User with lower Elo should be ranked second.");
        }
    }
}