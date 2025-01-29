namespace MTCG.Models
{
    public class MonsterCard : Card
    {
        public ElementType ElementType { get; set; }

        public MonsterCard(string id, string name, double damage)
            : base(id, name, damage)
        {
            ElementType = ParseElementType(name);
        }

        private ElementType ParseElementType(string name)
        {
            if (name.Contains("Water", StringComparison.OrdinalIgnoreCase))
                return ElementType.Water;
            if (name.Contains("Fire", StringComparison.OrdinalIgnoreCase))
                return ElementType.Fire;
            return ElementType.Normal; //default if there is no element mentioned in the name
        }

        public override void DisplayInfo()
        {
            Console.WriteLine($"MonsterCard: {Name}, Damage: {Damage}, Element: {ElementType}");
        }
    }
}