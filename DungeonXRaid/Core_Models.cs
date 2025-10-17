namespace DungeonXRaid.Core
{
    using DungeonXRaid.Items;
    using System.Text.Json.Serialization;

    public enum HeroClass { Warrior = 1, Mage = 2, Rogue = 3, Monk = 4 }

    
    public class StatBlock
    {
        public int STR { get; set; } = 0;   // Stärke – Nahkampf
        public int DEX { get; set; } = 0;   // Geschick – Treffer/Flucht
        public int INT { get; set; } = 0;   // Intelligenz – Magie
        public int VIT { get; set; } = 0;   // Vitalität – Lebenspunkte
        public int DEF { get; set; } = 0;   // Rüstung
        public int HPBonus { get; set; } = 0; // HP-Bonus

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
    }

    public enum EquipmentSlot { Weapon, Armor, Trinket, None }

    // Serialisierbares Itemmodell (für Boni / Loot)
    public class ItemModel
    {
        public string Name { get; set; } = "";
        public Rarity Rarity { get; set; } = Rarity.Common;
        public EquipmentSlot Slot { get; set; } = EquipmentSlot.None;
        public int Power { get; set; } = 0;
        public StatBlock Bonus { get; set; } = new StatBlock();
        public override string ToString() => $"{Name} [{Rarity}] (+{Power})";
    }

    public class EquipmentSet
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

        public ItemModel? Get(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => Weapon,
            EquipmentSlot.Armor => Armor,
            EquipmentSlot.Trinket => Trinket,
            _ => null
        };

        public void Set(EquipmentSlot slot, ItemModel? item)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: Weapon = item; break;
                case EquipmentSlot.Armor: Armor = item; break;
                case EquipmentSlot.Trinket: Trinket = item; break;
            }
        }
    }

    public class Character
    {
        public string Name { get; set; } = "Unnamed";
        public HeroClass Class { get; set; } = HeroClass.Rogue;

        // Basiswerte je Klasse
        public StatBlock Base { get; set; } = new StatBlock();

        // Ausrüstung & Inventar
        public EquipmentSet Equip { get; set; } = new EquipmentSet();
        public List<ItemModel> Inventory { get; set; } = new List<ItemModel>();

        // Meta
        public int Level { get; set; } = 1;
        public int Gold { get; set; } = 0;

        // HP (abgeleitet aus Stats)
        public int MaxHp { get; set; } = 100;
        public int Hp { get; set; } = 100;

        // ---- Startwerte je Klasse (anpassbar) ----
        public static StatBlock ClassDefaults(HeroClass cls) => cls switch
        {
            // ausbalancierte, klare Profile
            HeroClass.Warrior => new StatBlock { STR = 7, DEX = 3, INT = 1, VIT = 7, DEF = 2 },
            HeroClass.Mage => new StatBlock { STR = 1, DEX = 3, INT = 8, VIT = 3, DEF = 0 },
            HeroClass.Rogue => new StatBlock { STR = 3, DEX = 7, INT = 2, VIT = 3, DEF = 1 },
            HeroClass.Monk => new StatBlock { STR = 4, DEX = 5, INT = 3, VIT = 5, DEF = 1 },
            _ => new StatBlock()
        };

        // Fabrik: erzeugt Charakter mit Klassen-Startwerten
        public static Character New(string name, HeroClass @class)
        {
            var c = new Character
            {
                Name = name,
                Class = @class,
                Base = ClassDefaults(@class),
                Level = 1,
                Gold = 0
            };
            c.RecalculateDerived();
            c.Hp = c.MaxHp;
            return c;
        }

        // Gesamtwerte = Base + Boni aller ausgerüsteten Items
        public StatBlock TotalStats
        {
            get
            {
                var total = Base.Clone();
                foreach (var it in Equip.All) total += it.Bonus;
                return total;
            }
        }

        public void RecalculateDerived()
        {
            var t = TotalStats;
            MaxHp = 100 + t.VIT * 10 + t.HPBonus;
            if (Hp > MaxHp) Hp = MaxHp;
        }

        // Auto-Equip, wenn Item stärker ist
        public bool TryAutoEquip(ItemModel item, out ItemModel? replaced)
        {
            replaced = null;
            if (item.Slot == EquipmentSlot.None) return false;
            var current = Equip.Get(item.Slot);
            if (current == null || item.Power > current.Power)
            {
                replaced = current;
                Equip.Set(item.Slot, item);
                RecalculateDerived();
                return true;
            }
            return false;
        }
    }

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
}
