using NUnit.Framework;
using MTCG.Server;
using MTCG.Models;
using System;
using System.Collections.Generic;

namespace MTCG.Tests
{
    [TestFixture]
    public class DeckTests
    {
        private DBHandler _dbHandler;
        private string _testUsername;
        private string _testPackageId;
        private List<string> _testCardIds;

        [SetUp]
        public void Setup()
        {
            _dbHandler = new DBHandler(useTestDb: true); // ✅ Use test database
            (_testUsername, _testPackageId, _testCardIds) = TestDataSetup.InitializeTestData(_dbHandler); // ✅ Load shared test data
        }

        // Add a card to a user's deck
        [Test]
        public void AddCardToDeck_ShouldSucceed()
        {
            string testCardId = _testCardIds[0]; // Pick the first assigned card
            _dbHandler.DefineDeck(_testUsername, new List<string> { testCardId });

            var deckCards = _dbHandler.GetDeck(_testUsername);
            Assert.AreEqual(1, deckCards.Count, "The card should have been added to the deck.");
            Assert.AreEqual(testCardId, deckCards[0].Id, "The correct card should be in the deck.");
        }

        // Try to add a card that does not belong to the user
        [Test]
        public void AddCardToDeck_ShouldFailForWrongUser()
        {
            string secondUser = "testUser2_" + Guid.NewGuid();
            _dbHandler.CreateUser(secondUser, "password", 20, 100, "", "", "");

            string testCardId = _testCardIds[0]; // Pick a card owned by the first user

            Assert.Throws<Exception>(() =>
            {
                _dbHandler.DefineDeck(secondUser, new List<string> { testCardId });
            }, "User should not be able to add a card that does not belong to them.");
        }

        // Reset user's deck
        [Test]
        public void ResetDeck_ShouldRemoveAllCards()
        {
            _dbHandler.DefineDeck(_testUsername, _testCardIds); // Add all 5 cards to deck
            _dbHandler.ResetDeck(_testUsername); // Reset the deck

            var deckCards = _dbHandler.GetDeck(_testUsername);
            Assert.IsEmpty(deckCards, "The deck should be empty after reset.");
        }
    }
}
