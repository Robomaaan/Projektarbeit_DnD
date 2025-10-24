namespace DungeonXRaid.Core
{
    using DungeonXRaid.Items;
    using System.Text.Json.Serialization;

    public enum HeroClass { Warrior = 1, Mage = 2, Rogue = 3, Monk = 4 }
    public enum EquipmentSlot { Weapon, Armor, Trinket }

    public class StatBlock
    {
        public int STR { get; set; } = 0;
        public int DEX { get; set; } = 0;
        public int INT { get; set; } = 0;
        public int VIT { get; set; } = 0;
        public int DEF { get; set; } = 0;
        public int HPBonus { get; set; } = 0;

        public StatBlock Clone() => new StatBlock { STR = STR, DEX = DEX, INT = INT, VIT = VIT, DEF = DEF, HPBonus = HPBonus };
        public static StatBlock operator +(StatBlock a, StatBlock b) => new StatBlock
        { STR = a.STR + b.STR, DEX = a.DEX + b.DEX, INT = a.INT + b.INT, VIT = a.VIT + b.VIT, DEF = a.DEF + b.DEF, HPBonus = a.HPBonus + b.HPBonus };
        public void AddInPlace(StatBlock o) { STR += o.STR; DEX += o.DEX; INT += o.INT; VIT += o.VIT; DEF += o.DEF; HPBonus += o.HPBonus; }
    }

    public class Equipment
    {
        public ItemModel? Weapon { get; set; }
        public ItemModel? Armor { get; set; }
        public ItemModel? Trinket { get; set; }

        [JsonIgnore] public IEnumerable<ItemModel> All { get { if (Weapon != null) yield return Weapon; if (Armor != null) yield return Armor; if (Trinket != null) yield return Trinket; } }
        public StatBlock SumBonuses() { var s = new StatBlock(); foreach (var it in All) s.AddInPlace(it.Bonus); return s; }
        public ItemModel? GetBySlot(EquipmentSlot slot) => slot switch { EquipmentSlot.Weapon => Weapon, EquipmentSlot.Armor => Armor, EquipmentSlot.Trinket => Trinket, _ => null };
        public void SetBySlot(EquipmentSlot slot, ItemModel? item) { if (slot == EquipmentSlot.Weapon) Weapon = item; else if (slot == EquipmentSlot.Armor) Armor = item; else Trinket = item; }
    }

    public class Character
    {
        public string Name { get; set; } = "Hero";
        public HeroClass Class { get; set; } = HeroClass.Rogue;
        public int Level { get; set; } = 1;
        public int Gold { get; set; } = 0;

        public StatBlock Base { get; set; } = new StatBlock { STR = 2, DEX = 2, INT = 2, VIT = 2, DEF = 0 };
        public Equipment Equip { get; set; } = new Equipment();
        public List<ItemModel> Inventory { get; set; } = new List<ItemModel>();

        public int MaxHp { get; private set; } = 20;
        public int Hp { get; set; } = 20;

        [JsonIgnore] public StatBlock TotalStats => Base + Equip.SumBonuses();

        public static Character New(string name, HeroClass cls)
        {
            var c = new Character { Name = name, Class = cls };
            switch (cls)
            {
                case HeroClass.Warrior: c.Base = new StatBlock { STR = 4, DEX = 1, INT = 0, VIT = 3, DEF = 1 }; break;
                case HeroClass.Mage: c.Base = new StatBlock { STR = 0, DEX = 2, INT = 4, VIT = 2, DEF = 0 }; break;
                case HeroClass.Rogue: c.Base = new StatBlock { STR = 2, DEX = 4, INT = 1, VIT = 1, DEF = 0 }; break;
                case HeroClass.Monk: c.Base = new StatBlock { STR = 3, DEX = 3, INT = 0, VIT = 1, DEF = 0 }; break;
            }
            c.RecalculateDerived(forceFullHeal: true);
            return c;
        }

        // FIX: HP dürfen auf 0 fallen (nicht auf 1 clampen)
        public void RecalculateDerived(bool forceFullHeal = false)
        {
            var t = TotalStats;
            var newMax = Math.Max(1, 20 + (t.VIT * 5) + t.HPBonus);
            if (forceFullHeal)
            {
                MaxHp = newMax;
                Hp = newMax;
            }
            else
            {
                double ratio = MaxHp > 0 ? (double)Hp / MaxHp : 1.0;
                MaxHp = newMax;
                Hp = Math.Clamp((int)Math.Round(MaxHp * ratio), 0, MaxHp); // 0 erlaubt
            }
        }

        public bool TryAutoEquip(ItemModel item, out ItemModel? replaced)
        {
            replaced = null;
            var slot = item.Slot;
            var current = Equip.GetBySlot(slot);
            bool better = current == null || item.Power > current.Power;
            if (!better) return false;
            Equip.SetBySlot(slot, item);
            replaced = current;
            RecalculateDerived();
            return true;
        }
    }
}
