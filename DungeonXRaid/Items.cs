namespace DungeonXRaid.Items
{
    using DungeonXRaid.Core;

    public enum Rarity { Common, Rare, Epic, Legendary }

    public class ItemModel
    {
        public string Name { get; set; } = "Item";
        public Rarity Rarity { get; set; } = Rarity.Common;
        public EquipmentSlot Slot { get; set; } = EquipmentSlot.Weapon;
        public StatBlock Bonus { get; set; } = new StatBlock();
        public int Power { get; set; } = 0;
    }

    public static class LootTable
    {
        private static readonly (int weight, Rarity r)[] RarityWeights =
        {
            (60, Rarity.Common),
            (25, Rarity.Rare),
            (12, Rarity.Epic),
            ( 3, Rarity.Legendary)
        };

        private static readonly Random rng = new();

        public static ItemModel Roll(Random? r = null)
        {
            var rnd = r ?? rng;

            int total = 0; foreach (var (w, _) in RarityWeights) total += w;
            int pick = rnd.Next(total);
            Rarity rarity = Rarity.Common;
            foreach (var (w, rr) in RarityWeights) { if (pick < w) { rarity = rr; break; } pick -= w; }

            var slot = RollSlot(rnd);
            var (name, bonus, power) = GenerateBySlot(slot, rarity, rnd);

            return new ItemModel { Name = name, Rarity = rarity, Slot = slot, Bonus = bonus, Power = power };
        }

        private static EquipmentSlot RollSlot(Random rnd)
        {
            int r = rnd.Next(100);
            if (r < 45) return EquipmentSlot.Weapon;
            if (r < 80) return EquipmentSlot.Armor;
            return EquipmentSlot.Trinket;
        }

        private static (string name, StatBlock bonus, int power) GenerateBySlot(EquipmentSlot slot, Rarity rar, Random rnd)
        {
            int m = rar switch { Rarity.Common => 1, Rarity.Rare => 2, Rarity.Epic => 3, Rarity.Legendary => 4, _ => 1 };

            if (slot == EquipmentSlot.Weapon)
            {
                var weaponsCommon = new[] { "Knüppel", "Kurzschwert", "Dolch", "Axt", "Hirschfänger" };
                var weaponsRare = new[] { "Stahlklinge", "Kriegskeule", "Assassinenklinge", "Krummsäbel", "Kriegsbeil" };
                var weaponsEpic = new[] { "Runenklinge", "Drachenfaust", "Mondschneider", "Sturmspalter", "Blutdorn" };
                var weaponsLeg = new[] { "Klinge der Morgenröte", "Phönixschwert", "Sonnenlanze", "Echo der Titanen", "Nachtender" };

                var b = new StatBlock
                {
                    STR = rnd.Next(1, 2 + m),
                    DEX = rnd.Next(0, 1 + m / 2),
                    INT = rnd.Next(0, 1 + m / 2)
                };
                int p = b.STR * 3 + b.DEX + b.INT + m * 2;
                string n = rar switch
                {
                    Rarity.Common => weaponsCommon[rnd.Next(weaponsCommon.Length)],
                    Rarity.Rare => weaponsRare[rnd.Next(weaponsRare.Length)],
                    Rarity.Epic => weaponsEpic[rnd.Next(weaponsEpic.Length)],
                    Rarity.Legendary => weaponsLeg[rnd.Next(weaponsLeg.Length)],
                    _ => "Waffe"
                };
                return (n, b, p);
            }

            if (slot == EquipmentSlot.Armor)
            {
                var b = new StatBlock
                {
                    DEF = rnd.Next(1, 2 + m),
                    VIT = rnd.Next(0, 1 + m),
                    HPBonus = rnd.Next(0, 3 * m)
                };
                int p = b.DEF * 3 + b.VIT * 2 + b.HPBonus / 2 + m;
                string n = rar switch
                {
                    Rarity.Common => new[] { "Lederweste", "Abgenutzter Harnisch", "Gambeson" }[rnd.Next(3)],
                    Rarity.Rare => new[] { "Kettenhemd", "Gepanzerte Weste", "Schuppenpanzer" }[rnd.Next(3)],
                    Rarity.Epic => new[] { "Drachenleder", "Mondstahlrüstung", "Runenpanzer" }[rnd.Next(3)],
                    Rarity.Legendary => new[] { "Aegis des Lichts", "Phönixpanzer", "Sonnenharnisch" }[rnd.Next(3)],
                    _ => "Rüstung"
                };
                return (n, b, p);
            }

            // Schmuck
            {
                var b = new StatBlock
                {
                    STR = rnd.Next(0, m),
                    DEX = rnd.Next(0, m + 1),
                    INT = rnd.Next(0, m + 1),
                    VIT = rnd.Next(0, m),
                    HPBonus = rnd.Next(0, 2 * m)
                };
                int p = b.DEX + b.INT + b.VIT + b.STR + b.HPBonus / 2 + m;
                string n = rar switch
                {
                    Rarity.Common => new[] { "Kupferring", "Altes Amulett", "Glücksbringer" }[rnd.Next(3)],
                    Rarity.Rare => new[] { "Silberreif", "Runenanhänger", "Wanderstein" }[rnd.Next(3)],
                    Rarity.Epic => new[] { "Sternenreif", "Seelenfokus", "Echostein" }[rnd.Next(3)],
                    Rarity.Legendary => new[] { "Herz der Sonne", "Phönixfeder", "Zeitkristall" }[rnd.Next(3)],
                    _ => "Schmuck"
                };
                return (n, b, p);
            }
        }
    }
}
