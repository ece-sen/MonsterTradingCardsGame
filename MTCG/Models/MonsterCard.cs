namespace MTCG.Models
{
    public class MonsterCard : Card
    {
        public ElementType ElementType { get; set; }

        public MonsterCard(string name, int damage, ElementType elementType)
            : base(name, damage)
        {
            ElementType = elementType;
        }

        public override void DisplayInfo()
        {
            Console.WriteLine($"MonsterCard: {Name}, Damage: {Damage}, Element: {ElementType}");
        }

        public override int CalculateDamageAgainst(Card opponent)
        {
            if (opponent is SpellCard spell)
            {
                // Handle element-based damage adjustments
                if (ElementType == ElementType.Water && spell.EffectType == ElementType.Fire)
                    return Damage * 2; // Water is effective against Fire
                else if (ElementType == ElementType.Fire && spell.EffectType == ElementType.Water)
                    return Damage / 2; // Fire is not effective against Water
            }

            // Default damage calculation for Monster vs Monster
            return Damage;
        }
    }
}