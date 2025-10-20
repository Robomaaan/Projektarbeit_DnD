using DungeonXRaid.Core;
using DungeonXRaid.UI;
using System.Text;
using DungeonXRaid.Items;


namespace DungeonXRaid
{
    public static class DungeonGame
    {
        public static void Run(GameSession session)
        {
            ConsoleUI.ClearWithSize(110, 38);
            Console.Title = $"DungeonXRaid – {session.Hero.Name} ({session.Hero.Class})";
            Console.CursorVisible = false;

            const int MAP_W = 80, MAP_H = 28;
            var mapObj = new Map(MAP_W, MAP_H);
            var start = mapObj.GetRandomFloor();
            int px = start.x, py = start.y;
            var rng = new Random();

            bool running = true;
            while (running)
            {
                RenderFrame(session, mapObj, px, py);

                var key = Console.ReadKey(true).Key;
                int nx = px, ny = py;

                switch (key)
                {
                    case ConsoleKey.Escape:
                        running = false;
                        break;

                    case ConsoleKey.Enter:
                        if (mapObj.IsChest(px, py))
                        {
                            if (mapObj.TryOpenChest(px, py, out var loot))
                            {
                                var beforeStats = session.Hero.TotalStats;
                                int beforeMaxHp = session.Hero.MaxHp;

                                session.Hero.Inventory.Add(loot);

                                if (session.Hero.TryAutoEquip(loot, out var replaced))
                                {
                                    var afterStats = session.Hero.TotalStats;
                                    int afterMaxHp = session.Hero.MaxHp;

                                    string delta = BuildDeltaList(beforeStats, afterStats, beforeMaxHp, afterMaxHp);
                                    string bonus = ItemBonusShort(loot);

                                    var msg = $"Du rüstest automatisch aus:\n\n" +
                                              $"• {loot.Name} [{loot.Rarity}] (Power +{loot.Power})\n" +
                                              $"  Boni: {bonus}";
                                    if (replaced != null) msg += $"\nErsetzt: {replaced.Name} (Power +{replaced.Power})";
                                    msg += $"\n\nÄnderungen: {delta}";

                                    MessageBox.ShowCenter(msg);
                                }
                                else
                                {
                                    MessageBox.ShowCenter(
                                        $"Du findest:\n\n" +
                                        $"• {loot.Name} [{loot.Rarity}] (Power +{loot.Power})\n" +
                                        $"  Boni: {ItemBonusShort(loot)}\n\n(Item im Inventar)"
                                    );
                                }

                                ConsoleUI.ClearWithSize(110, 38);
                                RenderFrame(session, mapObj, px, py);
                            }
                        }
                        break;

                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow: nx--; break;
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow: nx++; break;
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow: ny--; break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow: ny++; break;
                }

                if (mapObj.IsWalkable(nx, ny))
                {
                    px = nx; py = ny;

                    // Zufalls-Encounter: 5% Chance pro Schritt
                    if (rng.Next(100) < 5)
                    {
                        try
                        {
                            if (!HandleEncounter(session))
                            {
                                // bei Niederlage: Session speichern & zurück
                                running = false;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Flucht erfolgreich: einfach weiterlaufen
                        }
                    }
                }
            }

            Console.CursorVisible = false;
            SaveSystem.Save(session);
            ConsoleUI.ClearWithSize();
            MessageBox.ShowCenter("Spielstand gespeichert.\n\n[Enter] zurück zum Hauptmenü");
            Console.CursorVisible = true;
        }

        private static bool HandleEncounter(GameSession session)
        {
            bool heroDied, won;
            int gold;
            Items.ItemModel? drop;

            won = Combat.Run(session.Hero, out heroDied, out gold, out drop);
            if (heroDied) return false; 

            // Nachkampf-HP-Check (nie negativ)
            if (session.Hero.Hp < 0) session.Hero.Hp = 0;
            return true;
        }

        // Rendering (ein Frame)
        private static void RenderFrame(GameSession session, Map mapObj, int px, int py)
        {
            var sb = new StringBuilder((mapObj.Width + 1) * (mapObj.Height + 16));

            var baseStats = session.Hero.Base;
            var totalStats = session.Hero.TotalStats;

            // Kopf/HUD
            sb.AppendLine(" Status ");
            sb.AppendLine(
                $"Name: {session.Hero.Name}   Klasse: {session.Hero.Class}   " +
                $"HP: {session.Hero.Hp}/{session.Hero.MaxHp}   Gold: {session.Hero.Gold}   Level: {session.Hero.Level}"
            );

            // Werte inkl. Bonusanteil in Klammern
            sb.AppendLine(
                $"STR: {FmtWithBonus(totalStats.STR, totalStats.STR - baseStats.STR),-8}" +
                $"DEX: {FmtWithBonus(totalStats.DEX, totalStats.DEX - baseStats.DEX),-8}" +
                $"INT: {FmtWithBonus(totalStats.INT, totalStats.INT - baseStats.INT),-8}" +
                $"VIT: {FmtWithBonus(totalStats.VIT, totalStats.VIT - baseStats.VIT),-8}" +
                $"DEF: {FmtWithBonus(totalStats.DEF, totalStats.DEF - baseStats.DEF),-8}" +
                $"Items: {session.Hero.Inventory.Count}"
            );

            // Ausrüstung + Kurzboni
            sb.AppendLine(
                $"Waffe: {ItemLine(session.Hero.Equip.Weapon),-24}" +
                $"Rüstung: {ItemLine(session.Hero.Equip.Armor),-28}" +
                $"Schmuck: {ItemLine(session.Hero.Equip.Trinket)}"
            );
            sb.AppendLine();

            // Karte
            int MAP_W = mapObj.Width, MAP_H = mapObj.Height;
            sb.Append(' ').Append(' ').Append(' ').AppendLine(new string('─', MAP_W + 2));
            for (int y = 0; y < MAP_H; y++)
            {
                sb.Append("   │");
                for (int x = 0; x < MAP_W; x++)
                {
                    if (x == px && y == py) { sb.Append('@'); continue; }
                    char tile = mapObj.GetTile(x, y);
                    if (tile == 'C') sb.Append('C'); else sb.Append(tile == '#' ? '#' : '.');
                }
                sb.AppendLine("│");
            }
            sb.Append(' ').Append(' ').Append(' ').AppendLine(new string('─', MAP_W + 2));

            if (mapObj.IsChest(px, py))
                sb.AppendLine("[Enter] Truhe öffnen   |   [Esc] zurück");
            else
                sb.AppendLine("[WASD] bewegen (5% Encounter)   |   [Esc] zurück");

            try { Console.SetCursorPosition(0, 0); } catch { }
            Console.Write(sb.ToString());
        }

        // Anzeige-Helfer
        private static string FmtWithBonus(int total, int bonus)
            => bonus != 0 ? $"{total} (+{bonus})" : $"{total}";

        private static string ItemBonusShort(ItemModel it)
        {
            var b = it.Bonus;
            var parts = new List<string>();
            void add(string k, int v) { if (v != 0) parts.Add($"+{k}{v}"); }
            add("STR", b.STR); add("DEX", b.DEX); add("INT", b.INT);
            add("VIT", b.VIT); add("DEF", b.DEF); if (b.HPBonus != 0) parts.Add($"+HP{b.HPBonus}");
            return parts.Count == 0 ? "—" : string.Join(", ", parts);
        }

        private static string ItemLine(ItemModel? it)
            => it == null ? "-" : $"{it.Name} ({ItemBonusShort(it)})";

        private static string BuildDeltaList(StatBlock before, StatBlock after, int beforeMaxHp, int afterMaxHp)
        {
            var parts = new List<string>();
            void add(string label, int a, int b)
            {
                int d = b - a;
                if (d != 0) parts.Add($"{label} {(d > 0 ? "+" : "")}{d}");
            }
            add(nameof(StatBlock.STR), before.STR, after.STR);
            add(nameof(StatBlock.DEX), before.DEX, after.DEX);
            add(nameof(StatBlock.INT), before.INT, after.INT);
            add(nameof(StatBlock.VIT), before.VIT, after.VIT);
            add(nameof(StatBlock.DEF), before.DEF, after.DEF);
            add("MaxHP", beforeMaxHp, afterMaxHp);
            return parts.Count == 0 ? "—" : string.Join(", ", parts);
        }
    }
}
