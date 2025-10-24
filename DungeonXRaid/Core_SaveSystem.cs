using System.Text.Json;

namespace DungeonXRaid.Core
{
    // einfache GameSession (Save-Container)
    public class GameSession
    {
        public Character Hero { get; set; }
        public string SaveName { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastPlayed { get; set; } = DateTime.Now;

        public string Id => SaveName; // Dateiname (ohne .json)

        public GameSession(Character hero)
        {
            Hero = hero;
            SaveName = hero.Name;
        }

        public static GameSession New(Character hero) => new GameSession(hero);
    }

    public static class SaveSystem
    {
        public static string SaveDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "DungeonXRaid", "saves");

        static SaveSystem() => Directory.CreateDirectory(SaveDir);

        static string PathFor(string id) => Path.Combine(SaveDir, $"{id}.json");

        public static void Save(GameSession session)
        {
            session.LastPlayed = DateTime.Now;
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PathFor(session.Id), json);
        }

        public static GameSession? Load(string id)
        {
            var path = PathFor(id);
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameSession>(json);
        }

        public static bool Delete(string id)
        {
            var path = PathFor(id);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        // für Menüs
        public static List<(string Id, string Display)> ListSaves()
        {
            var list = new List<(string Id, string Display)>();
            foreach (var file in Directory.GetFiles(SaveDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var s = JsonSerializer.Deserialize<GameSession>(json);
                    if (s != null)
                        list.Add((s.Id, $"{s.SaveName}  —  {s.Hero.Class}  —  {s.LastPlayed:g}"));
                }
                catch { }
            }

            return list
                .OrderByDescending(t => Load(t.Id)?.LastPlayed ?? DateTime.MinValue)
                .ToList();
        }

        public static GameSession LoadLastOrCreateDefault()
        {
            var saves = ListSaves();
            if (saves.Count > 0)
            {
                var lastId = saves.First().Id;
                var session = Load(lastId);
                if (session != null)
                    return session;
            }
            // Hier muss ggf. ein Default-Character erstellt werden
            var defaultHero = new Character { Name = "DefaultHero", Class = HeroClass.Warrior };
            return GameSession.New(defaultHero);
        }
    }
}
