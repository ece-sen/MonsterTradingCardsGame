using System;
using System.Collections.Generic;
using MTCG.Server;
using MTCG.Models;

namespace MTCG.Tests
{
    public static class TestDataSetup
    {
        private static bool _isInitialized = false;
        private static string _testUsername;
        private static string _testPackageId;
        private static List<string> _testCardIds = new();

        public static (string Username, string PackageId, List<string> CardIds) InitializeTestData(DBHandler dbHandler)
        {
            if (_isInitialized) return (_testUsername, _testPackageId, _testCardIds); // Prevent duplicate setup

            _testUsername = "testUser_" + Guid.NewGuid();
            dbHandler.CreateUser(_testUsername, "password", 20, 100, "", "", "");

            _testPackageId = Guid.NewGuid().ToString();

            // ✅ Create 5 cards
            for (int i = 0; i < 5; i++)
            {
                string cardId = Guid.NewGuid().ToString();
                _testCardIds.Add(cardId);
                dbHandler.AddCard(cardId, $"TestCard_{i}", 30 + (i * 5), "Normal", "Monster");
            }

            // ✅ Link the cards to the package
            foreach (var cardId in _testCardIds)
            {
                dbHandler.AddCardToPackage(_testPackageId, cardId);
            }

            // ✅ Assign package to user
            dbHandler.AssignPackageToUser(_testUsername, _testPackageId);

            _isInitialized = true; // Prevent multiple test setups
            return (_testUsername, _testPackageId, _testCardIds);
        }
    }
}