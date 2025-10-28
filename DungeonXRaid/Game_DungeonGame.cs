using DungeonXRaid.Core;
using DungeonXRaid.UI;
using DungeonXRaid.Items;
using DungeonXRaid.Anim;
using System.Text;

namespace DungeonXRaid
{
    public static class DungeonGame
    {
        private record MapEnemy(int X, int Y, string Name, int MaxHp, int Hp, int ATK, int DEF, int Gold, char Glyph, bool IsBoss);

        public static void Run(GameSession session)
        {
            ConsoleUI.ClearWithSize(110, 38);
            Console.Title = $"DungeonXRaid – {session.Hero.Name} ({session.Hero.Class})";
            Console.CursorVisible = false;

            const int MAP_W = 80, MAP_H = 28;
            var mapObj = new Map(MAP_W, MAP_H);
            var rng = new Random();

            // Gegner
            var enemies = SpawnStageEnemies(rng, mapObj, stage: 1);
            int bossCountdown = 0;
            bool bossSpawned = false;

            var start = mapObj.GetRandomFloor();
            int px = start.x, py = start.y;

            bool running = true;
            while (running)
            {
                RenderFrame(session, mapObj, px, py, enemies, bossCountdown, bossSpawned);

                var key = Console.ReadKey(true).Key;
                int nx = px, ny = py;

                switch (key)
                {
                    case ConsoleKey.Escape: running = false; break;
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow: nx--; break;
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow: nx++; break;
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow: ny--; break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow: ny++; break;
                }

                if (!mapObj.IsWalkable(nx, ny)) continue;

                // Bewegung
                px = nx; py = ny;

                // Auto-Open Chest
                if (mapObj.IsChest(px, py) && mapObj.TryOpenChest(px, py, out var loot))
                {
                    var before = session.Hero.TotalStats.Clone();
                    int beforeHP = session.Hero.MaxHp;

                    session.Hero.Inventory.Add(loot);
                    bool equipped = session.Hero.TryAutoEquip(loot, out var replaced);

                    string delta = BuildDeltaList(before, session.Hero.TotalStats, beforeHP, session.Hero.MaxHp);
                    ChestAnimation.ShowLoot(loot, equipped, replaced, delta, AnimSpeed.Normal);
                    ConsoleUI.ClearWithSize(110, 38);
                }

                // Kollision mit Gegner -> Kampf
                int idx = enemies.FindIndex(e => e.X == px && e.Y == py);
                if (idx >= 0)
                {
                    var e = enemies[idx];
                    if (RunMapCombat(session, e))
                    {
                        enemies.RemoveAt(idx);
                        if (enemies.Count == 0 && !bossSpawned)
                        {
                            bossCountdown = 25; // Schritte bis Boss
                            MessageBox.ShowCenter("Alle Feinde besiegt. Der Boss spürt deine Präsenz…");
                        }
                    }
                    else
                    {
                        running = false; // Tod/Abbruch
                    }
                }
                else
                {
                    // seltene Random-Encounters (1 %)
                    if (rng.Next(100) < 1)
                    {
                        bool cont = HandleRandomEncounter(session);
                        if (!cont) running = false;
                    }
                }

                // Boss-Countdown
                if (bossCountdown > 0)
                {
                    bossCountdown--;
                    if (bossCountdown == 0 && !bossSpawned)
                    {
                        var b = SpawnBoss(rng, mapObj, stage: 1);
                        enemies.Add(b);
                        bossSpawned = true;
                        MessageBox.ShowCenter("⚔ Ein dunkles Grollen… Der Boss ist erschienen!");
                    }
                }
            }

            Console.CursorVisible = false;
            SaveSystem.Save(session);
            ConsoleUI.ClearWithSize();
            MessageBox.ShowCenter("Spielstand gespeichert.\n\n[Enter] zurück zum Hauptmenü");
            Console.CursorVisible = true;
        }

        private static bool HandleRandomEncounter(GameSession session)
        {
            bool heroDied, won;
            int gold;
            ItemModel? drop;

            won = Core.Combat.Run(session.Hero, out heroDied, out gold, out drop);
            if (heroDied) return false;
            if (session.Hero.Hp < 0) session.Hero.Hp = 0;
            ConsoleUI.ClearWithSize(110, 38);
            return true;
        }

        private static bool RunMapCombat(GameSession session, MapEnemy e)
        {
            bool heroDied, won;
            int gold;
            ItemModel? drop;

            won = Core.Combat.Run(session.Hero, out heroDied, out gold, out drop);
            if (heroDied) return false;
            ConsoleUI.ClearWithSize(110, 38);
            return true;
        }

        private static List<MapEnemy> SpawnStageEnemies(Random rng, Map map, int stage)
        {
            int count = Math.Clamp((map.Width * map.Height) / 800, 6, 14);
            var list = new List<MapEnemy>();
            for (int i = 0; i < count; i++)
            {
                var pos = map.GetRandomFloor();
                int hp = 16 + rng.Next(8);
                int atk = 3 + rng.Next(3);
                int def = 9 + rng.Next(3);
                list.Add(new MapEnemy(pos.x, pos.y, "Mob", hp, hp, atk, def, 6 + rng.Next(6), 'e', false));
            }
            return list;
        }

        private static MapEnemy SpawnBoss(Random rng, Map map, int stage)
        {
            var pos = map.GetRandomFloor();
            return new MapEnemy(pos.x, pos.y, "BOSS", 80, 80, 10, 12, 50, 'B', true);
        }

        private static void RenderFrame(GameSession session, Map mapObj, int px, int py, List<MapEnemy> enemies, int bossCountdown, bool bossSpawned)
        {
            ConsoleUI.ClearWithSize(110, 38);
            var sb = new StringBuilder((mapObj.Width + 1) * (mapObj.Height + 16));

            var baseStats = session.Hero.Base;
            var totalStats = session.Hero.TotalStats;

            sb.AppendLine(" Status ");
            sb.AppendLine($"Name: {session.Hero.Name}   Klasse: {session.Hero.Class}   HP: {session.Hero.Hp}/{session.Hero.MaxHp}   Gold: {session.Hero.Gold}   Level: {session.Hero.Level}");

            sb.AppendLine(
                $"STR: {FmtWithBonus(totalStats.STR, totalStats.STR - baseStats.STR),-8}" +
                $"DEX: {FmtWithBonus(totalStats.DEX, totalStats.DEX - baseStats.DEX),-8}" +
                $"INT: {FmtWithBonus(totalStats.INT, totalStats.INT - baseStats.INT),-8}" +
                $"VIT: {FmtWithBonus(totalStats.VIT, totalStats.VIT - baseStats.VIT),-8}" +
                $"DEF: {FmtWithBonus(totalStats.DEF, totalStats.DEF - baseStats.DEF),-8}" +
                $"Items: {session.Hero.Inventory.Count}"
            );

            sb.AppendLine(
                $"Waffe: {ItemLine(session.Hero.Equip.Weapon),-24}" +
                $"Rüstung: {ItemLine(session.Hero.Equip.Armor),-28}" +
                $"Schmuck: {ItemLine(session.Hero.Equip.Trinket)}"
            );
            sb.AppendLine();

            int MAP_W = mapObj.Width, MAP_H = mapObj.Height;
            sb.Append("   ").AppendLine(new string('─', MAP_W + 2));
            for (int y = 0; y < MAP_H; y++)
            {
                sb.Append("   │");
                for (int x = 0; x < MAP_W; x++)
                {
                    if (x == px && y == py) { sb.Append('@'); continue; }

                    var en = enemies.FirstOrDefault(e => e.X == x && e.Y == y);
                    if (en != null) { sb.Append(en.IsBoss ? 'B' : 'E'); continue; }

                    char tile = mapObj.GetTile(x, y);
                    if (tile == 'C') sb.Append('C');
                    else sb.Append(tile == '#' ? '#' : '.');
                }
                sb.AppendLine("│");
            }
            sb.Append("   ").AppendLine(new string('─', MAP_W + 2));

            string extra = (bossCountdown > 0 && !bossSpawned) ? $"  |  Boss in {bossCountdown} Schritten" : "";
            sb.AppendLine($"[WASD] bewegen (2% Encounter){extra}    |    [Esc] Menü");

            Console.Write(sb.ToString());
        }

        private static string FmtWithBonus(int total, int bonus) =>
            bonus != 0 ? $"{total} ({(bonus > 0 ? "+" : "")}{bonus})" : $"{total}";

        private static string ItemLine(ItemModel? it)
        {
            if (it == null) return "-";
            var b = it.Bonus;
            var parts = new List<string>();
            if (b.STR != 0) parts.Add($"STR+{b.STR}");
            if (b.DEX != 0) parts.Add($"DEX+{b.DEX}");
            if (b.INT != 0) parts.Add($"INT+{b.INT}");
            if (b.VIT != 0) parts.Add($"VIT+{b.VIT}");
            if (b.DEF != 0) parts.Add($"DEF+{b.DEF}");
            if (b.HPBonus != 0) parts.Add($"HP+{b.HPBonus}");
            string bonus = parts.Count > 0 ? string.Join(",", parts) : "—";
            return $"{it.Name} [+{it.Power}] ({bonus})";
        }

        private static string BuildDeltaList(StatBlock before, StatBlock after, int beforeMaxHp, int afterMaxHp)
        {
            var diffs = new List<string>();
            void Add(string n, int a, int b) { int d = b - a; if (d != 0) diffs.Add($"{n}{(d > 0 ? "+" : "")}{d}"); }
            Add("STR", before.STR, after.STR);
            Add("DEX", before.DEX, after.DEX);
            Add("INT", before.INT, after.INT);
            Add("VIT", before.VIT, after.VIT);
            Add("DEF", before.DEF, after.DEF);
            Add("HP", beforeMaxHp, afterMaxHp);
            return diffs.Count == 0 ? "—" : string.Join(", ", diffs);
        }
    }
}
