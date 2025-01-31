using NUnit.Framework;
using MTCG.Server;
using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MTCG.Tests
{
    [TestFixture]
    public class TransactionTests
    {
        private DBHandler _dbHandler;
        private string _testUsername;
        private string _testPackageId;
        private List<string> _testCardIds;

        [SetUp]
        public void Setup()
        {
            _dbHandler = new DBHandler(useTestDb: true); // ✅ Use the test database

            // ✅ Ensure test data is always created before running tests
            (_testUsername, _testPackageId, _testCardIds) = TestDataSetup.InitializeTestData(_dbHandler);

            // ✅ Ensure at least one package exists before running tests
            if (string.IsNullOrEmpty(_dbHandler.GetAvailablePackage()))
            {
                string packageId = Guid.NewGuid().ToString();
                List<string> cardIds = new();

                for (int i = 0; i < 5; i++)
                {
                    string cardId = Guid.NewGuid().ToString();
                    cardIds.Add(cardId);
                    _dbHandler.AddCard(cardId, $"TestCard_{i}", 30 + (i * 5), "Normal", "Monster");
                }

                foreach (var cardId in cardIds)
                {
                    _dbHandler.AddCardToPackage(packageId, cardId);
                }
            }
        }

        // 1️⃣ Ensure package purchase works first
        [Test, Order(1)]
        public void PurchasePackage_ShouldAssignPackageIfAvailable()
        {
            Token._CreateTokenFor(new User { UserName = _testUsername });
            TransactionHandler transactionHandler = new();
            TcpClient fakeClient = new TcpClient();
            HttpSvrEventArgs fakeRequest = new(fakeClient, "POST /transactions/packages");

            transactionHandler.Handle(fakeRequest);

            var retrievedCards = _dbHandler.GetUserCards(_testUsername);
            Assert.AreEqual(5, retrievedCards.Count, "User should have received a full package of 5 cards!");
        }
        
        // 2️⃣ Test that a user cannot buy a package if they don’t have enough coins
        [Test, Order(3)]
        public void PurchasePackage_ShouldFailForInsufficientCoins()
        {
            string lowCoinsUser = "lowCoinsUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(lowCoinsUser, "password", 4, 100, "", "", ""); // Only 4 coins

            Token._CreateTokenFor(new User { UserName = lowCoinsUser });
            TransactionHandler transactionHandler = new();
            TcpClient fakeClient = new TcpClient(); // ✅ Fix: Provide a dummy TCP client
            HttpSvrEventArgs fakeRequest = new(fakeClient, "POST /transactions/packages");
            transactionHandler.Handle(fakeRequest);

            var user = _dbHandler.GetUser(lowCoinsUser);
            Assert.AreEqual(4, user.coins, "User should not have been able to buy a package!");
        }

        // 4️⃣ Test that a user with 0 coins cannot buy a package
        [Test, Order(4)]
        public void PurchasePackage_ShouldFailForZeroCoins()
        {
            string zeroCoinsUser = "zeroCoinsUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(zeroCoinsUser, "password", 0, 100, "", "", "");

            Token._CreateTokenFor(new User { UserName = zeroCoinsUser });
            TransactionHandler transactionHandler = new();

            TcpClient fakeClient = new TcpClient(); // ✅ Fix: Provide a dummy TCP client
            HttpSvrEventArgs fakeRequest = new(fakeClient, "POST /transactions/packages");

            transactionHandler.Handle(fakeRequest);

            var user = _dbHandler.GetUser(zeroCoinsUser);
            Assert.AreEqual(0, user.coins, "User with 0 coins should not be able to buy a package!");
        }
    }
}

