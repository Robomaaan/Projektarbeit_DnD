namespace DungeonXRaid.Core
{
    using DungeonXRaid.Items;
    using System.Text.Json.Serialization;

    // Klassen / Slots 

    public enum HeroClass { Warrior = 1, Mage = 2, Rogue = 3, Monk = 4 }

    public enum EquipmentSlot { Weapon, Armor, Trinket }

    // Statblöcke 
    public class StatBlock
    {
        public int STR { get; set; } = 0;   // Stärke – Nahkampf
        public int DEX { get; set; } = 0;   // Geschick – Treffer/Flucht
        public int INT { get; set; } = 0;   // Intelligenz – Magie
        public int VIT { get; set; } = 0;   // Vitalität – Lebenspunkte
        public int DEF { get; set; } = 0;   // Rüstung
        public int HPBonus { get; set; } = 0; // Zusätzliche Max-HP

        public StatBlock Clone() => new StatBlock
        {
            STR = STR,
            DEX = DEX,
            INT = INT,
            VIT = VIT,
            DEF = DEF,
            HPBonus = HPBonus
        };

        public static StatBlock operator +(StatBlock a, StatBlock b) => new StatBlock
        {
            STR = a.STR + b.STR,
            DEX = a.DEX + b.DEX,
            INT = a.INT + b.INT,
            VIT = a.VIT + b.VIT,
            DEF = a.DEF + b.DEF,
            HPBonus = a.HPBonus + b.HPBonus
        };

        public void AddInPlace(StatBlock other)
        {
            STR += other.STR;
            DEX += other.DEX;
            INT += other.INT;
            VIT += other.VIT;
            DEF += other.DEF;
            HPBonus += other.HPBonus;
        }
    }

    // Ausrüstung 

    public class Equipment
    {
        public ItemModel? Weapon { get; set; }
        public ItemModel? Armor { get; set; }
        public ItemModel? Trinket { get; set; }

        [JsonIgnore]
        public IEnumerable<ItemModel> All
        {
            get
            {
                if (Weapon != null) yield return Weapon;
                if (Armor != null) yield return Armor;
                if (Trinket != null) yield return Trinket;
            }
        }

        public StatBlock SumBonuses()
        {
            var sum = new StatBlock();
            foreach (var it in All)
                sum.AddInPlace(it.Bonus);
            return sum;
        }

        public ItemModel? GetBySlot(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => Weapon,
            EquipmentSlot.Armor => Armor,
            EquipmentSlot.Trinket => Trinket,
            _ => null
        };

        public void SetBySlot(EquipmentSlot slot, ItemModel? item)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: Weapon = item; break;
                case EquipmentSlot.Armor: Armor = item; break;
                case EquipmentSlot.Trinket: Trinket = item; break;
            }
        }
    }

    // Charakter 
    public class Character
    {
        public string Name { get; set; } = "Hero";
        public HeroClass Class { get; set; } = HeroClass.Rogue;

        public int Level { get; set; } = 1;
        public int Gold { get; set; } = 0;

        // Basiswerte (durch Klasse), Total = Basis + Ausrüstung
        public StatBlock Base { get; set; } = new StatBlock { STR = 2, DEX = 2, INT = 2, VIT = 2, DEF = 0 };

        public Equipment Equip { get; set; } = new Equipment();

        // Inventar (alle gefundenen Items)
        public List<ItemModel> Inventory { get; set; } = new List<ItemModel>();

        // Abgeleitete Werte
        public int MaxHp { get; private set; } = 20;
        public int Hp { get; set; } = 20;

        [JsonIgnore]
        public StatBlock TotalStats => Base + Equip.SumBonuses();

        // Fabrik 
        public static Character New(string name, HeroClass cls)
        {
            var c = new Character { Name = name, Class = cls };
            // Startwerte je Klasse
            switch (cls)
            {
                case HeroClass.Warrior:
                    c.Base = new StatBlock { STR = 4, DEX = 1, INT = 0, VIT = 3, DEF = 1 };
                    break;
                case HeroClass.Mage:
                    c.Base = new StatBlock { STR = 0, DEX = 2, INT = 4, VIT = 2, DEF = 0 };
                    break;
                case HeroClass.Rogue:
                    c.Base = new StatBlock { STR = 2, DEX = 4, INT = 1, VIT = 1, DEF = 0 };
                    break;
                case HeroClass.Monk:
                    c.Base = new StatBlock { STR = 3, DEX = 3, INT = 0, VIT = 1, DEF = 0 };
                    break;
            }
            c.RecalculateDerived(forceFullHeal: true);
            return c;
        }

        // HP/MaxHP u.ä. aus Stats neu berechnen
        public void RecalculateDerived(bool forceFullHeal = false)
        {
            var t = TotalStats;
            // einfache HP-Formel: Grund 20 + 5*VIT + Bonus
            var newMax = Math.Max(1, 20 + (t.VIT * 5) + t.HPBonus);
            if (forceFullHeal)
            {
                MaxHp = newMax;
                Hp = newMax;
            }
            else
            {
                // Relatives Verhältnis grob halten (nicht unter 1 fallen)
                double ratio = MaxHp > 0 ? (double)Hp / MaxHp : 1.0;
                MaxHp = newMax;
                Hp = Math.Clamp((int)Math.Round(MaxHp * ratio), 1, MaxHp);
            }
        }

        // Versucht Auto-Equip: legt Item in passenden Slot, wenn „besser“ (Power)
        public bool TryAutoEquip(ItemModel item, out ItemModel? replaced)
        {
            replaced = null;

            var slot = item.Slot;
            var current = Equip.GetBySlot(slot);

            // „Besser“: höhere Power; bei Gleichstand lassen wir aktuelles Item
            bool better = current == null || item.Power > current.Power;
            if (!better) return false;

            Equip.SetBySlot(slot, item);
            replaced = current;

            // total neu berechnen
            RecalculateDerived();
            return true;
        }
    }

    // Spielstand 

    public class GameSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SaveName { get; set; } = "Spielstand";
        public Character Hero { get; set; } = Character.New("Rogue", HeroClass.Rogue);
        public DateTime LastPlayed { get; set; } = DateTime.Now;

        public static GameSession New(Character hero)
        {
            return new GameSession
            {
                SaveName = hero.Name,
                Hero = hero,
                LastPlayed = DateTime.Now
            };
        }
    }

    // Gegner-Modelle & Fabrik 

    public class EnemyModel
    {
        public string Name { get; set; } = "Goblin";
        public int MaxHp { get; set; } = 20;
        public int Hp { get; set; } = 20;
        public int ATK { get; set; } = 3;   // Angriffs-Bonus (zu d20 addiert)
        public int DEF { get; set; } = 10;  // Zielwert, der zum Treffen überschritten werden muss
        public char Glyph { get; set; } = 'g';
        public int GoldReward { get; set; } = 5;
    }

    public static class EnemyFactory
    {
        private static readonly Random rng = new();

        public static EnemyModel CreateForLevel(int level)
        {
            // Grundtypen; werden mit Level leicht skaliert
            var pool = new (string name, char glyph, int baseHp, int atk, int def, int gold)[]
            {
                ("Kleiner Goblin",   'g', 20, 2,  9,  5),
                ("Höhlenfledermaus", 'b', 18, 3, 10,  6),
                ("Skelett",          's', 22, 3, 11,  7),
                ("Dunkelwolf",       'w', 26, 4, 11,  9),
                ("Schattenspuk",     'x', 24, 5, 12, 12),
            };

            var pick = pool[rng.Next(pool.Length)];
            int hp = pick.baseHp + Math.Max(0, level - 1) * 3;
            int atk = pick.atk + Math.Max(0, (level - 1) / 2);
            int def = pick.def + Math.Max(0, (level - 1) / 2);
            int gold = pick.gold + Math.Max(0, level - 1);

            return new EnemyModel
            {
                Name = pick.name,
                Glyph = pick.glyph,
                MaxHp = hp,
                Hp = hp,
                ATK = atk,
                DEF = def,
                GoldReward = gold
            };
        }
    }
}
