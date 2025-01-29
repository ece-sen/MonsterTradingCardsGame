using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTCG.Models;
using MTCG.Server;

public class BattleHandler : Handler, IHandler
{
    private static readonly object _battleLock = new();
    private static Queue<string> _battleQueue = new();

    public override bool Handle(HttpSvrEventArgs e)
    {
        if (e.Path.TrimEnd('/', ' ', '\t') == "/battles" && e.Method == "POST")
        {
            return InitiateBattle(e);
        }
        return false;
    }

    private bool InitiateBattle(HttpSvrEventArgs e)
    {
        (bool success, User? user) auth = Token.Authenticate(e);
        if (!auth.success || auth.user is null)
        {
            e.Reply(HttpStatusCode.UNAUTHORIZED, "Unauthorized: Invalid token.");
            return true;
        }

        string username = auth.user.UserName;
        DBHandler dbHandler = new();

        lock (_battleLock)
        {
            if (_battleQueue.Count == 0)
            {
                _battleQueue.Enqueue(username);
                Console.WriteLine($"{username} is waiting for an opponent...");
                Monitor.Wait(_battleLock);
            }
            else
            {
                string opponent = _battleQueue.Dequeue();
                Monitor.Pulse(_battleLock);
                Task.Run(() => RunBattle(username, opponent, dbHandler));
            }
        }
        return true;
    }

    private void RunBattle(string player1, string player2, DBHandler dbHandler)
    {
        Console.WriteLine($"Battle started between {player1} and {player2}");
        List<Card> deck1 = dbHandler.GetDeck(player1);
        List<Card> deck2 = dbHandler.GetDeck(player2);
        
        if (deck1.Count < 4 || deck2.Count < 4)
        {
            Console.WriteLine("Both players must have 4 cards in their deck to battle.");
            return;
        }

        int rounds = 0;
        while (deck1.Count > 0 && deck2.Count > 0 && rounds < 100)
        {
            rounds++;
            Card card1 = deck1[new Random().Next(deck1.Count)];
            Card card2 = deck2[new Random().Next(deck2.Count)];
            Console.WriteLine($"Round {rounds}: {card1.Name} ({card1.Damage}) vs {card2.Name} ({card2.Damage})");
            ResolveRound(card1, card2, deck1, deck2);
        }

        string winner = deck1.Count > deck2.Count ? player1 : player2;
        string loser = winner == player1 ? player2 : player1;
        
        dbHandler.UpdateUser(winner, elo: dbHandler.GetUser(winner)?.elo + 3);
        dbHandler.UpdateUser(loser, elo: dbHandler.GetUser(loser)?.elo - 5);
        Console.WriteLine($"Battle ended. Winner: {winner}");
    }

    private void ResolveRound(Card card1, Card card2, List<Card> deck1, List<Card> deck2)
    {
        double damage1 = ApplyElementEffect(card1, card2);
        double damage2 = ApplyElementEffect(card2, card1);
        
        Console.WriteLine($"{card1.Name} ({damage1}) vs {card2.Name} ({damage2})");

        if (damage1 > damage2)
        {
            deck2.Remove(card2);
            deck1.Add(card2);
        }
        else if (damage2 > damage1)
        {
            deck1.Remove(card1);
            deck2.Add(card1);
        }
    }

    private double ApplyElementEffect(Card attacker, Card defender)
    {
        if (attacker is MonsterCard m1 && defender is MonsterCard m2)
            return attacker.Damage;

        if (attacker is SpellCard spell)
        {
            switch (spell.ElementType)
            {
                case ElementType.Water when defender is MonsterCard { ElementType: ElementType.Fire }:
                case ElementType.Fire when defender is MonsterCard { ElementType: ElementType.Normal }:
                case ElementType.Normal when defender is MonsterCard { ElementType: ElementType.Water }:
                    return attacker.Damage * 2;

                case ElementType.Fire when defender is MonsterCard { ElementType: ElementType.Water }:
                case ElementType.Normal when defender is MonsterCard { ElementType: ElementType.Fire }:
                case ElementType.Water when defender is MonsterCard { ElementType: ElementType.Normal }:
                    return attacker.Damage / 2;
            }
        }
        return attacker.Damage;
    }
}
