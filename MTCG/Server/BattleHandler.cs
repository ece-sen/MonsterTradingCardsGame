using System.Text.Json.Nodes;
using MTCG.Models;

namespace MTCG.Server;

public class BattleHandler : Handler, IHandler
{
    private static readonly object _battleLock = new();
    private static Queue<string> _battleQueue = new();
    public List<String> battleList = new();


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
                battleList.Add($"{username} is waiting for an opponent...");
                Monitor.Wait(_battleLock);
            }
            else
            {
                string opponent = _battleQueue.Dequeue();
                Monitor.Pulse(_battleLock);
                RunBattle(username, opponent, dbHandler);
            }
        }

        var reply = new JsonObject()
        {
            ["success"] = true,
            ["message"] = "Battle has finished.",
            ["battle_log"] = new JsonArray(battleList.Select(log => JsonValue.Create(log)).ToArray())
        };
        
        // Send the final battle log with http 200
        var status = HttpStatusCode.OK;
        e.Reply(status, reply.ToString()); 
        return true;
    }

    private void RunBattle(string player1, string player2, DBHandler dbHandler)
    {
        battleList.Add($"Battle started between {player1} and {player2}");
        
        List<Card> deck1 = dbHandler.GetDeck(player1);
        List<Card> deck2 = dbHandler.GetDeck(player2);
    
        if (deck1.Count < 4 || deck2.Count < 4)
        {
            battleList.Add("Both players must have 4 cards in their deck to battle.");
            return;
        }

        int rounds = 0;
        while (deck1.Count > 0 && deck2.Count > 0 && rounds < 100)
        {
            rounds++;
            Card card1 = deck1[new Random().Next(deck1.Count)];
            Card card2 = deck2[new Random().Next(deck2.Count)];
            battleList.Add($"Round {rounds}: {card1.Name} ({card1.Damage}) vs {card2.Name} ({card2.Damage})");
            ResolveRound(card1, card2, deck1, deck2);
        }

        bool isDraw = deck1.Count == deck2.Count;

        if (!isDraw)
        {
            string winner = deck1.Count > deck2.Count ? player1 : player2;
            string loser = winner == player1 ? player2 : player1;

            dbHandler.UpdateUser(winner, elo: dbHandler.GetUser(winner)?.Elo + 3);
            dbHandler.UpdateUser(loser, elo: dbHandler.GetUser(loser)?.Elo - 5);

            dbHandler.UpdateGameStats(winner, won: true, lost: false);
            dbHandler.UpdateGameStats(loser, won: false, lost: true);

            battleList.Add($"Battle ended. Winner: {winner}");
        }
        else
        {
            battleList.Add("The battle ended in a draw. No ELO changes.");
            dbHandler.UpdateGameStats(player1, won: false, lost: false);
            dbHandler.UpdateGameStats(player2, won: false, lost: false);
        }
    }

    private void ResolveRound(Card card1, Card card2, List<Card> deck1, List<Card> deck2)
    {
        // for unique feature
        Random rng = new Random();
        
        // Special Cases
        if (card1 is MonsterCard goblin && card2 is MonsterCard dragon1 && goblin.Name.Contains("Goblin") && dragon1.Name.Contains("Dragon"))
        {
            battleList.Add($"{card1.Name} is too afraid to attack {card2.Name}!");
            return; // Goblin does nothing
        }

        if (card1 is MonsterCard ork && card2 is MonsterCard wizard && ork.Name.Contains("Ork") && wizard.Name.Contains("Wizard"))
        {
            battleList.Add($"{card2.Name} controls {card1.Name}, preventing it from attacking!");
            return;
        }

        if (card1 is MonsterCard knight && card2 is SpellCard waterSpell && waterSpell.ElementType == ElementType.Water && knight.Name.Contains("Knight"))
        {
            battleList.Add($"{card1.Name} drowns instantly due to {card2.Name}!");
            deck1.Remove(card1);
            return;
        }

        if (card1 is MonsterCard kraken && card2 is SpellCard)
        {
            battleList.Add($"{card1.Name} is immune to spells!");
            return;
        }

        if (card1 is MonsterCard fireElf && card2 is MonsterCard dragon && fireElf.Name.Contains("FireElf") && dragon.Name.Contains("Dragon"))
        {
            battleList.Add($"{card1.Name} evades {card2.Name}'s attack!");
            return;
        }

        // Normal Round Resolution
        double damage1 = ApplyElementEffect(card1, card2);
        double damage2 = ApplyElementEffect(card2, card1);
        
        // Simple Critical Hit Feature (10% chance for each card)
        bool crit1 = rng.Next(1, 101) <= 10;
        bool crit2 = rng.Next(1, 101) <= 10;
        
        if (crit1)
        {
            damage1 *= 2;
            battleList.Add($"CRITICAL HIT! {card1.Name} deals double damage ({damage1}) to {card2.Name}!");
        }

        if (crit2)
        {
            damage2 *= 2;
            battleList.Add($"CRITICAL HIT! {card2.Name} deals double damage ({damage2}) to {card1.Name}!");
        }
        
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