using NUnit.Framework;
using MTCG.Server;
using MTCG.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace MTCG.Tests
{
    [TestFixture]
    public class BattleTests
    {
        private BattleHandler _battleHandler;
        private DBHandler _dbHandler;
        private DeckHandler _deckHandler;

        [SetUp]
        public void Setup()
        {
            _battleHandler = new BattleHandler();
            _dbHandler = new DBHandler(useTestDb: true);
            _deckHandler = new DeckHandler();

            // Reset test database before each test
            _dbHandler.ClearDatabase();

            // Create test users
            _dbHandler.CreateUser("Player1", "password", 20, 100, "", "", "");
            _dbHandler.CreateUser("Player2", "password", 20, 100, "", "", "");

            // Create unique package IDs
            string packageIdP1 = Guid.NewGuid().ToString();
            string packageIdP2 = Guid.NewGuid().ToString();

            // Add 5 cards to Player1's package
            string goblinId = Guid.NewGuid().ToString();
            string orkId = Guid.NewGuid().ToString();
            string knightId = Guid.NewGuid().ToString();
            string krakenId = Guid.NewGuid().ToString();
            string extraCardP1 = Guid.NewGuid().ToString(); // This extra card won't be in the deck

            _dbHandler.AddCard(goblinId, "Goblin", 10, "Normal", "Monster");
            _dbHandler.AddCard(orkId, "Ork", 15, "Fire", "Monster");
            _dbHandler.AddCard(knightId, "Knight", 20, "Water", "Monster");
            _dbHandler.AddCard(krakenId, "Kraken", 25, "Normal", "Monster");
            _dbHandler.AddCard(extraCardP1, "ExtraCardP1", 5, "Normal", "Monster"); // This will NOT be in the deck

            _dbHandler.AddCardToPackage(packageIdP1, goblinId);
            _dbHandler.AddCardToPackage(packageIdP1, orkId);
            _dbHandler.AddCardToPackage(packageIdP1, knightId);
            _dbHandler.AddCardToPackage(packageIdP1, krakenId);
            _dbHandler.AddCardToPackage(packageIdP1, extraCardP1);

            // Add 5 cards to Player2's package
            string dragonId = Guid.NewGuid().ToString();
            string wizardId = Guid.NewGuid().ToString();
            string waterSpellId = Guid.NewGuid().ToString();
            string fireElfId = Guid.NewGuid().ToString();
            string extraCardP2 = Guid.NewGuid().ToString(); // This extra card won't be in the deck

            _dbHandler.AddCard(dragonId, "Dragon", 30, "Fire", "Monster");
            _dbHandler.AddCard(wizardId, "Wizard", 5, "Normal", "Monster");
            _dbHandler.AddCard(waterSpellId, "Water Spell", 12, "Water", "Spell");
            _dbHandler.AddCard(fireElfId, "FireElf", 10, "Fire", "Monster");
            _dbHandler.AddCard(extraCardP2, "ExtraCardP2", 5, "Normal", "Monster"); // This will NOT be in the deck

            _dbHandler.AddCardToPackage(packageIdP2, dragonId);
            _dbHandler.AddCardToPackage(packageIdP2, wizardId);
            _dbHandler.AddCardToPackage(packageIdP2, waterSpellId);
            _dbHandler.AddCardToPackage(packageIdP2, fireElfId);
            _dbHandler.AddCardToPackage(packageIdP2, extraCardP2);

            // Assign packages to users
            _dbHandler.AssignPackageToUser("Player1", packageIdP1);
            _dbHandler.AssignPackageToUser("Player2", packageIdP2);

            // Fetch user's cards (all 5 from the package)
            var player1Cards = _dbHandler.GetUserCards("Player1").Select(c => c.Id).ToList();
            var player2Cards = _dbHandler.GetUserCards("Player2").Select(c => c.Id).ToList();

            // Ensure we only use the first 4 cards in the deck
            _dbHandler.DefineDeck("Player1", player1Cards.Take(4).ToList());
            _dbHandler.DefineDeck("Player2", player2Cards.Take(4).ToList());
        }
        
        [Test]
        public void Test_Goblin_And_Dragon_Remain_In_Deck()
        {
            // Arrange: Get the decks after setup
            var deck1 = _dbHandler.GetDeck("Player1");
            var deck2 = _dbHandler.GetDeck("Player2");

            // Find the specific cards
            var goblin = deck1.First(c => c.Name == "Goblin");
            var dragon = deck2.First(c => c.Name == "Dragon");

            int initialDeck1Size = deck1.Count;
            int initialDeck2Size = deck2.Count;

            // Act: Resolve a battle round between Goblin and Dragon
            _battleHandler.ResolveRound(goblin, dragon, deck1, deck2);

            // Assert: Check that no cards were removed from the decks
            Assert.AreEqual(initialDeck1Size, deck1.Count, "Goblin should not be removed from deck.");
            Assert.AreEqual(initialDeck2Size, deck2.Count, "Dragon should not be removed from deck.");
        }
        [Test]
        public void Test_Knight_Drowns_From_WaterSpell()
        {
            // Arrange: Get decks after setup
            var deck1 = _dbHandler.GetDeck("Player1");
            var deck2 = _dbHandler.GetDeck("Player2");

            // Find the Knight and Water Spell
            var knight = deck1.First(c => c.Name == "Knight");
            var waterSpell = deck2.First(c => c.Name == "Water Spell");

            int initialDeck1Size = deck1.Count;

            // Act: Resolve round
            _battleHandler.ResolveRound(knight, waterSpell, deck1, deck2);

            // Assert: Knight should be removed from deck1
            Assert.AreEqual(initialDeck1Size - 1, deck1.Count, "Knight should be removed due to drowning.");
            Assert.That(_battleHandler.battleList, Has.Some.Contains("Knight drowns instantly due to Water Spell!"));
        }

        [Test]
        public void Test_FireElf_Evades_Dragon_Attack()
        {
            // Arrange: Get the decks after setup
            var deck1 = _dbHandler.GetDeck("Player1");
            var deck2 = _dbHandler.GetDeck("Player2");

            // Find the specific cards
            var fireElf = deck2.First(c => c.Name == "FireElf");
            var dragon = deck2.First(c => c.Name == "Dragon");

            int initialDeckSize = deck2.Count;

            // Act: Resolve a battle round between FireElf and Dragon
            _battleHandler.ResolveRound(fireElf, dragon, deck2, deck2);

            // Assert: FireElf should still be in the deck (evades attack)
            Assert.AreEqual(initialDeckSize, deck2.Count, "FireElf should not be removed since it evades Dragon.");
            Assert.That(_battleHandler.battleList, Has.Some.Contains("FireElf evades Dragon's attack!"));
        }
        [Test]
        public void Test_Kraken_Immune_To_Spells()
        {
            // Arrange: Get decks after setup
            var deck1 = _dbHandler.GetDeck("Player1");
            var deck2 = _dbHandler.GetDeck("Player2");

            // Find the Kraken and Water Spell
            var kraken = deck1.First(c => c.Name == "Kraken");
            var spell = deck2.First(c => c is SpellCard); // Any spell

            int initialDeck1Size = deck1.Count;

            // Act: Resolve round
            _battleHandler.ResolveRound(kraken, spell, deck1, deck2);

            // Assert: Kraken should still be in the deck
            Assert.AreEqual(initialDeck1Size, deck1.Count, "Kraken should remain in deck because it is immune to spells.");
            Assert.That(_battleHandler.battleList, Has.Some.Contains("Kraken is immune to spells!"));
        }

    }
}