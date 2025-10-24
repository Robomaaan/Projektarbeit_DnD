namespace DungeonXRaid.Core
{
    public class MetaProgress
    {
        public int BankGold { get; set; }
        public int STR { get; set; }
        public int DEX { get; set; }
        public int INT { get; set; }
        public int VIT { get; set; }
        public int DEF { get; set; }

        public int CostFor(string stat) => stat switch
        {
            "STR" => 20 + STR * 10,
            "DEX" => 20 + DEX * 10,
            "INT" => 20 + INT * 10,
            "VIT" => 30 + VIT * 15,
            "DEF" => 25 + DEF * 12,
            _ => 50
        };
    }

    public static class MetaStore
    {
        private static readonly string MetaPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonXRaid", "meta.json");

        public static MetaProgress Load()
        {
            try
            {
                if (File.Exists(MetaPath))
                {
                    var json = File.ReadAllText(MetaPath);
                    return System.Text.Json.JsonSerializer.Deserialize<MetaProgress>(json) ?? new MetaProgress();
                }
            }
            catch { }
            return new MetaProgress();
        }

        public static void Save(MetaProgress mp)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MetaPath)!);
            var json = System.Text.Json.JsonSerializer.Serialize(mp, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MetaPath, json);
        }

        public static void ApplyTo(Character c, MetaProgress mp)
        {
            c.Base.STR += mp.STR;
            c.Base.DEX += mp.DEX;
            c.Base.INT += mp.INT;
            c.Base.VIT += mp.VIT;
            c.Base.DEF += mp.DEF;
            c.RecalculateDerived(forceFullHeal: true);
        }
    }
}
