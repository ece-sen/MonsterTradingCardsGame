namespace MTCG.Models;

public abstract class Card
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double Damage { get; set; }

    public Card(string id, string name, double damage)
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
}
