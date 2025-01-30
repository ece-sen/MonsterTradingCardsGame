using NUnit.Framework;
using MTCG.Server;
using MTCG.Models;
using System;

namespace MTCG.Tests
{
    [TestFixture]
    public class UserTests
    {
        private DBHandler _dbHandler;

        [SetUp]
        public void Setup()
        {
            _dbHandler = new DBHandler(useTestDb: true); // Connect to test DB
        }


        // 1️⃣ Test user creation with unique usernames
        [Test]
        public void CreateUser_ShouldStoreUserCorrectly()
        {
            string uniqueUsername = "testUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(uniqueUsername, "password", 20, 100, "", "", "");

            var user = _dbHandler.GetUser(uniqueUsername);
            Assert.NotNull(user);
            Assert.AreEqual(uniqueUsername, user.UserName);
            Assert.AreEqual(20, user.coins);
            Assert.AreEqual(100, user.elo);
        }

        // 2️⃣ Test login with correct credentials using a unique username
        [Test]
        public void Logon_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            string uniqueUsername = "validUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(uniqueUsername, "securePass", 20, 100, "", "", "");

            var (success, token) = User.Logon(uniqueUsername, "securePass");

            Assert.IsTrue(success);
            Assert.IsNotEmpty(token);
        }

        // 3️⃣ Test login with incorrect password using a unique username
        [Test]
        public void Logon_ShouldFail_WhenPasswordIsIncorrect()
        {
            string uniqueUsername = "wrongPassUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(uniqueUsername, "rightPass", 20, 100, "", "", "");

            var (success, token) = User.Logon(uniqueUsername, "wrongPass");

            Assert.IsFalse(success);
            Assert.IsEmpty(token);
        }

        // 4️⃣ Test retrieving a non-existent user with a unique username
        [Test]
        public void GetUser_ShouldReturnNull_WhenUserDoesNotExist()
        {
            string uniqueUsername = "nonexistentUser_" + Guid.NewGuid();
            var user = _dbHandler.GetUser(uniqueUsername);
            Assert.IsNull(user);
        }

        // 5️⃣ Test updating user stats using a unique username
        [Test]
        public void UpdateUser_ShouldModifyCoinsAndElo()
        {
            string uniqueUsername = "updateUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(uniqueUsername, "pass", 20, 100, "", "", "");
            _dbHandler.UpdateUser(uniqueUsername, coins: 50, elo: 200);

            var user = _dbHandler.GetUser(uniqueUsername);
            Assert.NotNull(user);
            Assert.AreEqual(50, user.coins);
            Assert.AreEqual(200, user.elo);
        }
    }
}
