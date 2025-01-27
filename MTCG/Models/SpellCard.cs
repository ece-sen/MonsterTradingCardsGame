namespace MTCG.Models;

public class SpellCard : Card
{
    public ElementType EffectType { get; set; }

    public SpellCard(string id, string name, int damage, ElementType effectType) : base(id, name, damage)
    {
        EffectType = effectType;
    }

    public override void DisplayInfo()
    {
        Console.WriteLine($"Name: {Name}, Damage: {Damage}, Effect: {EffectType}");
    }

    public override int CalculateDamageAgainst(Card opponent)
    {
        if (opponent is MonsterCard monster)
        {
            if(EffectType == ElementType.Water && monster.ElementType == ElementType.Fire && monster.ElementType == ElementType.Fire)
                return Damage * 2;
            
            else if (EffectType == ElementType.Fire && monster.ElementType == ElementType.Water)
                return Damage / 2;
        }
        
        return Damage;
    }
}