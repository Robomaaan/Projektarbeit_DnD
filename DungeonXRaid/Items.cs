namespace DungeonXRaid.Items
{
    public enum Rarity { Common, Rare, Epic, Legendary }

    public abstract class Item
    {
        public string Name { get; }
        public Rarity Rarity { get; }

        protected Item(string name, Rarity rarity)
        {
            Name = name; Rarity = rarity;
        }
    }
}
