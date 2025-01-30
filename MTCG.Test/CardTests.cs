using NUnit.Framework;
using MTCG.Models;
using MTCG.Server;
using System;
using System.IO;
using System.Collections.Generic;

namespace MTCG.Tests
{
    [TestFixture]
    public class CardTests
    {
        private DBHandler _dbHandler;
        private string _testUsername;
        private string _testPackageId;
        private List<string> _testCardIds;

        [SetUp]
        public void Setup()
        {
            _dbHandler = new DBHandler(useTestDb: true); // Use test database
            (_testUsername, _testPackageId, _testCardIds) = TestDataSetup.InitializeTestData(_dbHandler); // ✅ Use shared test data
        }

        // 1️⃣ Test creating a MonsterCard
        [Test]
        public void MonsterCard_ShouldAssignCorrectElementType()
        {
            var card = new MonsterCard("1", "FireDragon", 50);
            Assert.AreEqual(ElementType.Fire, card.ElementType);
        }

        // 2️⃣ Test creating a SpellCard
        [Test]
        public void SpellCard_ShouldAssignCorrectElementType()
        {
            var card = new SpellCard("2", "WaterBlast", 40);
            Assert.AreEqual(ElementType.Water, card.ElementType);
        }

        // 3️⃣ Test DisplayInfo() outputs correctly for MonsterCard
        [Test]
        public void MonsterCard_DisplayInfo_ShouldOutputCorrectInfo()
        {
            var card = new MonsterCard("3", "Goblin", 30);
            using (var output = new StringWriter())
            {
                Console.SetOut(output);
                card.DisplayInfo();
                string result = output.ToString();
                Assert.IsTrue(result.Contains("Goblin"));
                Assert.IsTrue(result.Contains("30"));
            }
        }

        // 4️⃣ Test DisplayInfo() outputs correctly for SpellCard
        [Test]
        public void SpellCard_DisplayInfo_ShouldOutputCorrectInfo()
        {
            var card = new SpellCard("4", "LightningBolt", 60);
            using (var output = new StringWriter())
            {
                Console.SetOut(output);
                card.DisplayInfo();
                string result = output.ToString();
                Assert.IsTrue(result.Contains("LightningBolt"));
                Assert.IsTrue(result.Contains("60"));
            }
        }

        // 5️⃣ Test retrieving stored cards from the database
        [Test]
        public void GetUserCards_ShouldReturnCardsForUser()
        {
            var retrievedCards = _dbHandler.GetUserCards(_testUsername);
            Assert.AreEqual(5, retrievedCards.Count, "User should have 5 cards assigned!");
            foreach (var cardId in _testCardIds)
            {
                Assert.IsTrue(retrievedCards.Exists(c => c.Id == cardId), $"Card {cardId} was not found!");
            }
        }

        // 6️⃣ Test retrieving cards for a non-existent user
        [Test]
        public void GetUserCards_ShouldReturnEmptyForNonexistentUser()
        {
            var retrievedCards = _dbHandler.GetUserCards("nonexistentUser");
            Assert.IsEmpty(retrievedCards);
        }

        // 7️⃣ Test that a user cannot access another user's card
        [Test]
        public void CardBelongsToUser_ShouldReturnFalseForWrongUser()
        {
            string otherUser = "testUser2_" + Guid.NewGuid();
            _dbHandler.CreateUser(otherUser, "pass", 20, 100, "", "", "");

            string testCardId = _testCardIds[0]; // Select the first card

            bool belongsToOtherUser = _dbHandler.CardBelongsToUser(otherUser, testCardId);
            Assert.IsFalse(belongsToOtherUser, "Another user should NOT have access to this card!");
        }
    }
}
