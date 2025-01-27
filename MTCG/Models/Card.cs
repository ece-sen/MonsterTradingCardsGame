namespace MTCG.Models;

public abstract class Card
{
    public string Id { get; set; } 
    public string Name {get; set;}
    public int Damage {get; set;}

    public Card(string id, string name, int damage)
    {
        Id = id;
        Name = name;
        Damage = damage;
    }
    
    //Common method for displaying card info
    public virtual void DisplayInfo()
    {
        Console.WriteLine($"{Name}: Damage: {Damage}");
    }
    
    // Battle logic helpers
    public abstract int CalculateDamageAgainst(Card opponent);
}
