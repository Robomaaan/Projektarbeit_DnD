using DungeonXRaid.UI;
using DungeonXRaid.Items;
using System.Text;

namespace DungeonXRaid.Core
{
    public static class Combat
    {
        private static readonly Random rng = new();

        // Führt einen kompletten Kampf durch. true = Sieg, false = Niederlage/Abbruch.
        public static bool Run(Character hero, out bool heroDied, out int goldGained, out ItemModel? itemDrop)
        {
            heroDied = false;
            goldGained = 0;
            itemDrop = null;

            var enemy = EnemyFactory.CreateForLevel(hero.Level);

            Console.CursorVisible = false;
            ConsoleUI.ClearWithSize(110, 38);
            MessageBox.ShowCenter($"Ein {enemy.Name} erscheint!\n\nBereit für den Kampf? [Enter]");

            var log = new Queue<string>();
            void Log(string s) { if (log.Count > 8) log.Dequeue(); log.Enqueue(s); }

            while (enemy.Hp > 0 && hero.Hp > 0)
            {
                // Spielerzug
                int d20 = Roll(20);
                int attack = d20 + hero.TotalStats.STR;
                bool hit = attack >= enemy.DEF;
                int dmg = hit ? Math.Max(1, Roll(6) + hero.TotalStats.STR) : 0;
                if (hit) enemy.Hp = Math.Max(0, enemy.Hp - dmg);

                RenderPanel(hero, enemy, "Du greifst an!", d20, hit ? dmg : 0, true, log);
                Log(hit ? $"Treffer! (d20 {d20} + STR {hero.TotalStats.STR} ≥ {enemy.DEF}) → {dmg} Schaden"
                         : $"Daneben… (d20 {d20} + STR {hero.TotalStats.STR} < {enemy.DEF})");
                if (!WaitStep()) return true; // Flucht

                if (enemy.Hp <= 0) break;

                // Gegnerzug
                int d20e = Roll(20);
                int targetDef = 10 + hero.TotalStats.DEF;
                bool ehit = (d20e + enemy.ATK) >= targetDef;
                int edmg = ehit ? Math.Max(1, Roll(6) + enemy.ATK / 2) : 0;
                if (ehit)
                {
                    hero.Hp = Math.Max(0, hero.Hp - edmg);
                    hero.RecalculateDerived();
                }

                RenderPanel(hero, enemy, $"{enemy.Name} greift an!", d20e, ehit ? edmg : 0, false, log);
                Log(ehit ? $"{enemy.Name} trifft! (d20 {d20e} + ATK {enemy.ATK} ≥ {targetDef}) → {edmg} Schaden"
                          : $"{enemy.Name} verfehlt. (d20 {d20e} + ATK {enemy.ATK} < {targetDef})");
                if (!WaitStep()) return true; // Flucht
            }

            if (hero.Hp <= 0)
            {
                heroDied = true;
                MessageBox.ShowCenter($"Du wurdest vom {enemy.Name} besiegt …");
                return false;
            }

            // Sieg
            goldGained = enemy.GoldReward;
            hero.Gold += goldGained;

            if (rng.Next(100) < 30)
            {
                itemDrop = LootTable.Roll(rng);
                hero.Inventory.Add(itemDrop);
                hero.TryAutoEquip(itemDrop, out _);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Sieg gegen {enemy.Name}!");
            sb.AppendLine($"Beute: +{goldGained} Gold");
            if (itemDrop != null) sb.AppendLine($"Drop: {itemDrop.Name} [{itemDrop.Rarity}] (+{itemDrop.Power})");
            MessageBox.ShowCenter(sb.ToString());
            return true;
        }

        // Rendering 

        private static void RenderPanel(Character hero, EnemyModel enemy, string title, int dRoll, int dmg, bool playerTurn, IEnumerable<string> log)
        {
            ConsoleUI.ClearWithSize(110, 38);
            int W = Math.Min(Console.WindowWidth, 110);
            int H = Math.Min(Console.WindowHeight, 38);

            ConsoleUI.DrawBox(2, 1, W - 4, H - 2, $"KAMPF – {title}");

            int boxW = (W - 10) / 2;
            int boxH = 14;

            ConsoleUI.DrawBox(4, 3, boxW, boxH, $"{hero.Name}  HP {hero.Hp}/{hero.MaxHp}");
            ConsoleUI.DrawBox(6 + boxW, 3, boxW, boxH, $"{enemy.Name}  HP {enemy.Hp}/{enemy.MaxHp}");

            RenderHeroArt(6, 5);
            RenderEnemyArt(8 + boxW, 5, enemy.Glyph);

            ConsoleUI.DrawBox(4, 3 + boxH + 1, W - 8, 10, "Würfel");
            DrawDice(6, 3 + boxH + 3, 7, dRoll);
            DrawDmgBar(22, 3 + boxH + 3, dmg, playerTurn);

            ConsoleUI.DrawBox(4, 3 + boxH + 1 + 10, W - 8, H - (3 + boxH + 1 + 10) - 3, "Kampflog");
            int ly = 3 + boxH + 1 + 12;
            foreach (var line in log) { ConsoleUI.Write(6, ly, line); ly++; }

            ConsoleUI.Write(6, H - 3, "[Enter/Nächstes]  [Esc/Fliehen]");
        }

        private static void DrawDmgBar(int x, int y, int dmg, bool playerTurn)
        {
            string who = playerTurn ? "Spieler Schaden" : "Gegner Schaden";
            ConsoleUI.Write(x, y, $"{who}: {(dmg > 0 ? dmg.ToString() : "—")}");
            int barLen = Math.Clamp(dmg, 0, 20);
            ConsoleUI.Write(x, y + 1, new string('█', barLen));
        }

        private static void DrawDice(int x, int y, int size, int value)
        {
            for (int i = 0; i < size; i++)
            {
                ConsoleUI.Put(x + i, y, i == 0 ? '┌' : (i == size - 1 ? '┐' : '─'));
                ConsoleUI.Put(x + i, y + size - 1, i == 0 ? '└' : (i == size - 1 ? '┘' : '─'));
                if (i > 0 && i < size - 1)
                {
                    ConsoleUI.Put(x, y + i, '│');
                    ConsoleUI.Put(x + size - 1, y + i, '│');
                }
            }
            string center = $"d20:{value}";
            int cx = x + Math.Max(0, (size - center.Length) / 2);
            int cy = y + size / 2;
            ConsoleUI.Write(cx, cy, center);
        }

        private static int Roll(int sides) => rng.Next(1, sides + 1);

        /// <returns>true, wenn weiter (Enter) – false, wenn Flucht (Esc & Erfolg)</returns>
        private static bool WaitStep()
        {
            while (true)
            {
                var k = ConsoleUI.ReadKey().Key;
                if (k == ConsoleKey.Enter) return true;
                if (k == ConsoleKey.Escape)
                {
                    if (rng.Next(100) < 50) { MessageBox.ShowCenter("Du entkommst erfolgreich!"); return false; }
                    else { MessageBox.ShowCenter("Flucht fehlgeschlagen!"); return true; }
                }
            }
        }

        private static void RenderHeroArt(int x, int y)
        {
            string[] art = { @"  \O/ ", @"   |  ", @"  / \ ", @"  / \ " };
            for (int i = 0; i < art.Length; i++) ConsoleUI.Write(x, y + i, art[i]);
        }

        private static void RenderEnemyArt(int x, int y, char glyph)
        {
            string[] gob = { @"  __ ", @" (..) ", @" /||\ ", @"  /\  " };
            string[] bat = { @" \  / ", @"  \/  ", @" /\/\ ", @"  /\  " };
            string[] wolf = { @" /\_/\ ", @"( o.o )", @" > ^ < ", @"       " };
            string[] ske = { @"  __  ", @" [__] ", @"  /\  ", @"  /\  " };

            string[] use = glyph switch { 'b' => bat, 'w' => wolf, 's' => ske, _ => gob };
            for (int i = 0; i < use.Length; i++) ConsoleUI.Write(x, y + i, use[i]);
        }
    }
}
