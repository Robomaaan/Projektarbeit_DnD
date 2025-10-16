using System.Text.Json;

namespace DungeonXRaid.Core
{
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
                catch { /* ignore */ }
            }

            return list
                .OrderByDescending(t => Load(t.Id)?.LastPlayed ?? DateTime.MinValue)
                .ToList();
        }

        public static GameSession LoadLastOrCreateDefault()
        {
            var all = ListSaves();
            if (all.Count > 0)
            {
                var s = Load(all.First().Id);
                if (s != null) return s;
            }
            var def = GameSession.New(new Character { Name = "Rogue", Class = HeroClass.Rogue });
            Save(def);
            return def;
        }
    }
}
