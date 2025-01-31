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
            _dbHandler = new DBHandler(useTestDb: true); // ✅ Use the test database
        }

        // 1️⃣ Test that the scoreboard orders users by Elo rating
        [Test]
        public void Scoreboard_ShouldOrderUsersByElo()
        {
            string user1 = "user1_" + Guid.NewGuid();
            string user2 = "user2_" + Guid.NewGuid();

            _dbHandler.CreateUser(user1, "password", 20, 300, "", "", ""); // Higher Elo
            _dbHandler.CreateUser(user2, "password", 20, 250, "", "", ""); // Lower Elo

            var scoreboard = _dbHandler.GetScoreboard();

            Assert.AreEqual(user1, scoreboard[0].UserName, "User with higher Elo should be ranked first.");
            Assert.AreEqual(user2, scoreboard[1].UserName, "User with lower Elo should be ranked second.");
        }
    }
}