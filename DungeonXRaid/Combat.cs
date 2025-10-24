using DungeonXRaid.UI;
using DungeonXRaid.Items;
using System.Text;

namespace DungeonXRaid.Core
{
    public static class Combat
    {
        private static readonly Random rng = new();

        public static bool Run(Character hero, out bool heroDied, out int goldGained, out ItemModel? itemDrop)
        {
            heroDied = false; goldGained = 0; itemDrop = null;

            var enemy = EnemyModels.ForLevel(hero.Level);
            Console.CursorVisible = false;
            ConsoleUI.ClearWithSize(110, 38);
            MessageBox.ShowCenter($"Ein {enemy.Name} erscheint!\n\nBereit für den Kampf? [Enter]");

            var log = new Queue<string>();
            void Log(string s) { if (log.Count > 8) log.Dequeue(); log.Enqueue(s); }

            while (enemy.Hp > 0 && hero.Hp > 0)
            {
                // ---- Spielerzug ----
                var ui = RenderPanel(hero, enemy, "Du greifst an!", showRollPlaceholder: true);
                int d20 = AnimateDiceRoll(ui.diceX, ui.diceY, ui.diceSize, faces: 20, durationMs: 2000);
                int attack = d20 + hero.TotalStats.STR;
                bool hit = attack >= enemy.DEF;
                int dmg = hit ? Math.Max(1, Roll(6) + hero.TotalStats.STR) : 0;
                if (hit) enemy.Hp = Math.Max(0, enemy.Hp - dmg);

                RenderPanel(hero, enemy, $"Du greifst an! (d20:{d20})", dmg, true, log);
                Log(hit ? $"Treffer! (d20 {d20} + STR {hero.TotalStats.STR} ≥ {enemy.DEF}) → {dmg} Schaden"
                         : $"Daneben… (d20 {d20} + STR {hero.TotalStats.STR} < {enemy.DEF})");
                if (!WaitStep()) return true; // Flucht

                if (enemy.Hp <= 0) break;

                // ---- Gegnerzug ----
                ui = RenderPanel(hero, enemy, $"{enemy.Name} greift an!", showRollPlaceholder: true);
                int d20e = AnimateDiceRoll(ui.diceX, ui.diceY, ui.diceSize, faces: 20, durationMs: 2000);
                int targetDef = 10 + hero.TotalStats.DEF;
                bool ehit = (d20e + enemy.ATK) >= targetDef;
                int edmg = ehit ? Math.Max(1, Roll(6) + enemy.ATK / 2) : 0;
                if (ehit)
                {
                    hero.Hp = Math.Max(0, hero.Hp - edmg);  // 0 möglich
                    hero.RecalculateDerived();              // behält 0 bei
                }

                RenderPanel(hero, enemy, $"{enemy.Name} greift an! (d20:{d20e})", edmg, false, log);
                Log(ehit ? $"{enemy.Name} trifft! (d20 {d20e} + ATK {enemy.ATK} ≥ {targetDef}) → {edmg} Schaden"
                          : $"{enemy.Name} verfehlt. (d20 {d20e} + ATK {enemy.ATK} < {targetDef})");
                if (!WaitStep()) return true;

                if (hero.Hp <= 0) break;
            }

            if (hero.Hp <= 0)
            {
                heroDied = true;
                ConsoleUI.ClearWithSize(110, 38);
                MessageBox.ShowCenter($"Du wurdest vom {enemy.Name} besiegt …");
                return false;
            }

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
            ConsoleUI.ClearWithSize(110, 38);
            MessageBox.ShowCenter(sb.ToString());
            return true;
        }

        // ---------- Rendering & Animation ----------

        private static (int diceX, int diceY, int diceSize) RenderPanel(Character hero, EnemyModel enemy, string title, int dmg = 0, bool playerTurn = true, IEnumerable<string>? log = null, bool showRollPlaceholder = false)
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
            int diceX = 6, diceY = 3 + boxH + 3, diceSize = 7;
            DrawDiceBox(diceX, diceY, diceSize);
            if (!showRollPlaceholder) ConsoleUI.Write(diceX + 1, diceY + diceSize - 2, "d20:—");

            DrawDmgBar(22, 3 + boxH + 3, dmg, playerTurn);

            int logX = 4, logY = 3 + boxH + 1 + 10, logW = W - 8, logH = H - (3 + boxH + 1 + 10) - 3;
            ConsoleUI.DrawBox(logX, logY, logW, logH, "Kampflog");
            if (log != null)
            {
                int ly = logY + 2;
                int inner = Math.Max(1, logW - 4);
                foreach (var line in log)
                {
                    string t = line.Length > inner ? line[..inner] : line;
                    ConsoleUI.Write(logX + 2, ly, t);
                    ly++;
                    if (ly >= logY + logH - 1) break;
                }
            }

            ConsoleUI.Write(6, H - 3, "[Enter/Nächstes]  [Esc/Fliehen]");
            return (diceX, diceY, diceSize);
        }

        private static void DrawDiceBox(int x, int y, int size)
        {
            for (int i = 0; i < size; i++)
            {
                ConsoleUI.Put(x + i, y, i == 0 ? '┌' : (i == size - 1 ? '┐' : '─'));
                ConsoleUI.Put(x + i, y + size - 1, i == 0 ? '└' : (i == size - 1 ? '┘' : '─'));
            }
            for (int j = 1; j < size - 1; j++)
            {
                ConsoleUI.Put(x, y + j, '│');
                ConsoleUI.Put(x + size - 1, y + j, '│');
            }
        }

        // echte Sicht-Animation (~2s): Zahlen laufen im Würfel und werden langsamer
        private static int AnimateDiceRoll(int x, int y, int size, int faces, int durationMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int last = rng.Next(1, faces + 1);

            while (sw.ElapsedMilliseconds < durationMs)
            {
                last = rng.Next(1, faces + 1);
                ConsoleUI.Write(x + 1, y + size - 2, "       ");
                ConsoleUI.Write(x + 1, y + size - 2, $"d20:{last}");

                double p = Math.Clamp(sw.ElapsedMilliseconds / (double)durationMs, 0, 1);
                int delay = (int)(20 + 120 * p * p); // ease-out
                Thread.Sleep(delay);
            }
            sw.Stop();

            ConsoleUI.Write(x + 1, y + size - 2, "       ");
            ConsoleUI.Write(x + 1, y + size - 2, $"d20:{last}");
            return last;
        }

        private static void DrawDmgBar(int x, int y, int dmg, bool playerTurn)
        {
            string who = playerTurn ? "Spieler Schaden" : "Gegner Schaden";
            ConsoleUI.Write(x, y, $"{who}: {(dmg > 0 ? dmg.ToString() : "—")}");
            int barLen = Math.Clamp(dmg, 0, 20);
            ConsoleUI.WithColor(playerTurn ? ConsoleColor.Green : ConsoleColor.Red, () =>
            {
                ConsoleUI.Write(x, y + 1, new string('█', barLen));
            });
        }

        private static int Roll(int faces) => rng.Next(1, faces + 1);

        // EnemyModel minimal
        public sealed class EnemyModel
        {
            public string Name { get; }
            public int MaxHp { get; }
            public int Hp { get; set; }
            public int ATK { get; }
            public int DEF { get; }
            public int GoldReward { get; }
            public char Glyph { get; }

            public EnemyModel(string name, int hp, int atk, int def, int gold, char glyph)
            { Name = name; MaxHp = hp; Hp = hp; ATK = atk; DEF = def; GoldReward = gold; Glyph = glyph; }
        }

        private static class EnemyModels
        {
            private static readonly EnemyModel[] Pool = new[]
            {
                new EnemyModel("Dunkelwolf", 26, 4, 11, 9, 'W'),
                new EnemyModel("Goblin",     20, 3, 10, 7, 'G'),
                new EnemyModel("Nachtschatten",22,5,11,10,'N'),
            };

            public static EnemyModel ForLevel(int lvl)
            {
                var p = Pool[rng.Next(Pool.Length)];
                int bonus = Math.Max(0, lvl - 1);
                return new EnemyModel(p.Name, p.MaxHp + bonus * 4, p.ATK + bonus, p.DEF + bonus, p.GoldReward + 3 * bonus, p.Glyph);
            }
        }

        private static bool WaitStep()
        {
            while (true)
            {
                var k = ConsoleUI.ReadKey().Key;
                if (k == ConsoleKey.Escape) return false;
                if (k == ConsoleKey.Enter) return true;
            }
        }

        private static void RenderHeroArt(int x, int y)
        {
            ConsoleUI.Write(x, y + 0, @"\O/");
            ConsoleUI.Write(x, y + 1, @" | ");
            ConsoleUI.Write(x, y + 2, @"/\ ");
            ConsoleUI.Write(x, y + 3, @"/ \");
        }
        private static void RenderEnemyArt(int x, int y, char glyph)
        {
            ConsoleUI.Write(x, y + 0, @" /\ /\");
            ConsoleUI.Write(x, y + 1, @"( o.o )");
            ConsoleUI.Write(x, y + 2, @" > ^ < ");
        }
    }
}
