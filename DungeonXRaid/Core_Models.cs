namespace DungeonXRaid.Core
{
    public enum HeroClass { Warrior = 1, Mage = 2, Rogue = 3, Monk = 4 }

    public class Character
    {
        public string Name { get; set; } = "Unnamed";
        public HeroClass Class { get; set; } = HeroClass.Rogue;
        public int Level { get; set; } = 1;
        public int MaxHp { get; set; } = 100;
        public int Hp { get; set; } = 100;
        public int Gold { get; set; } = 0;
    }

    public class GameSession
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
        public string SaveName { get; set; } = "Spielstand";
        public Character Hero { get; set; } = new Character();
        public System.DateTime LastPlayed { get; set; } = System.DateTime.Now;

        public static GameSession New(Character hero) => new GameSession
        {
            SaveName = hero.Name,
            Hero = hero,
            LastPlayed = System.DateTime.Now
        };
    }
}
