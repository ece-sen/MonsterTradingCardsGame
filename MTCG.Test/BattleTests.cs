using NUnit.Framework;
using MTCG.Server;
using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MTCG.Tests
{
    [TestFixture]
    public class BattleTests
    {
        private DBHandler _dbHandler;
        private string _testUsername;
        private string _opponentUsername;
        private string _testPackageId;
        private string _opponentPackageId;
        private List<string> _testUserCardIds;
        private List<string> _opponentCardIds;
        private BattleHandler _battleHandler;

        [SetUp]
        public void Setup()
        {
            _dbHandler = new DBHandler(useTestDb: true); 

            // create test users
            _testUsername = "testUser_" + Guid.NewGuid();
            _opponentUsername = "opponentUser_" + Guid.NewGuid();
            _dbHandler.CreateUser(_testUsername, "password", 20, 100, "", "", "");
            _dbHandler.CreateUser(_opponentUsername, "password", 20, 100, "", "", "");

            // create unique package IDs
            _testPackageId = Guid.NewGuid().ToString();
            _opponentPackageId = Guid.NewGuid().ToString();

            _testUserCardIds = new List<string>();
            _opponentCardIds = new List<string>();

            // create a deck for test user
            for (int i = 1; i <= 4; i++)
            {
                string cardId = Guid.NewGuid().ToString();
                _testUserCardIds.Add(cardId);
                _dbHandler.AddCard(cardId, $"TestUserCard_{i}", 30 + (i * 5), "Normal", "Monster");
            }

            // Create a deck cards for opponent
            for (int i = 1; i <= 4; i++)
            {
                string cardId = Guid.NewGuid().ToString();
                _opponentCardIds.Add(cardId);
                _dbHandler.AddCard(cardId, $"OpponentCard_{i}", 30 + (i * 5), "Normal", "Monster");
            }

            foreach (var cardId in _testUserCardIds)
            {
                _dbHandler.AddCardToPackage(_testPackageId, cardId);
            }

            foreach (var cardId in _opponentCardIds)
            {
                _dbHandler.AddCardToPackage(_opponentPackageId, cardId);
            }

            // Assign packages to users
            _dbHandler.AssignPackageToUser(_testUsername, _testPackageId);
            _dbHandler.AssignPackageToUser(_opponentUsername, _opponentPackageId);

            if (_dbHandler.GetUserCards(_testUsername).Count < 4 || _dbHandler.GetUserCards(_opponentUsername).Count < 4)
            {
                throw new Exception($"One or both players do not have enough cards to battle.");
            }

            _dbHandler.DefineDeck(_testUsername, _testUserCardIds);
            _dbHandler.DefineDeck(_opponentUsername, _opponentCardIds);

            _battleHandler = new BattleHandler();
        }

        // battle starts when two players join the queue
        [Test]
        public void BattleStartsWhenTwoPlayersAreInQueue()
        {
            Token._CreateTokenFor(new User { UserName = _testUsername });
            Token._CreateTokenFor(new User { UserName = _opponentUsername });

            TcpClient fakeClient1 = new TcpClient();
            TcpClient fakeClient2 = new TcpClient();

            HttpSvrEventArgs fakeRequest1 = new(fakeClient1, "POST /battles");
            HttpSvrEventArgs fakeRequest2 = new(fakeClient2, "POST /battles");

            _battleHandler.Handle(fakeRequest1);
            bool battleStarted = _battleHandler.Handle(fakeRequest2);

            Assert.IsTrue(battleStarted, "Battle should start when two players join the queue.");
        }
    }
}
