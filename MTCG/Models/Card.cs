namespace MTCG.Models;

public abstract class Card
{
    public string Name {get; set;}
    public int Damage {get; set;}

    public Card(string name, int damage)
    {
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
