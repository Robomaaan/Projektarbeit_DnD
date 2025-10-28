using System.Runtime.InteropServices;
using DungeonXRaid.UI;
using DungeonXRaid.Items;


namespace DungeonXRaid.Anim
{
    public enum AnimSpeed { Slow, Normal, Fast }

    public static class ChestAnimation
    {

        public static void ShowLoot(
            ItemModel loot,
            bool equipped = false,
            ItemModel? replaced = null,
            string? delta = null,
            AnimSpeed speed = AnimSpeed.Normal)
        {
            Console.CursorVisible = false;
            ConsoleUI.ClearWithSize(110, 38);

            // Fenster layouten
            int w = Math.Clamp(74, 40, Math.Max(40, Console.WindowWidth - 8));
            int h = 22;
            int x = Math.Max(0, (Console.WindowWidth - w) / 2);
            int y = Math.Max(0, (Console.WindowHeight - h) / 2);

            ConsoleUI.DrawBox(x, y, w, h, "Truhe");

            int innerLeft = x + 2;
            int innerTop = y + 2;
            int innerW = w - 4;
            int innerH = h - 4;

            // Timing
            var (frameDelay, textDelay, sparkleFrames) = BaseTimingsFor(speed);

            // Frames 
            var frames = ChestFrames();

            // Helper: zentriert einen Chest-Frame zeichnen
            void DrawFrame(string[] art)
            {
                int artW = art.Max(l => l.Length);
                int artH = art.Length;
                int fx = innerLeft + Math.Max(0, (innerW - artW) / 2);
                int fy = innerTop + 2;

                // Canvas (oberer Bereich) leeren
                int cleanRows = innerH - 1;
                for (int j = 0; j < cleanRows; j++)
                    ConsoleUI.Write(innerLeft, innerTop + j, new string(' ', innerW));

                for (int i = 0; i < artH; i++)
                    ConsoleUI.Write(fx, fy + i, art[i]);
            }

            // Truhen-Opening abspielen 
            foreach (var f in frames)
            {
                DrawFrame(f);
                if (SleepSkippable(frameDelay)) break;
            }

            // Sparkle/Glitzer nach dem Öffnen
            Sparkle(innerLeft, innerTop + 1, innerW, 6, sparkleFrames, Math.Max(15, frameDelay / 2));

            // Item „springt“ hoch 
            string itemLine = $"• {loot.Name} [{loot.Rarity}] (Power +{loot.Power})  –  {ItemBonusShort(loot)}";
            int startY = y + h - 4;
            int targetY = innerTop + 1;

            FlyInCentered(itemLine, innerLeft, innerTop, innerW, startY, targetY, textDelay, LootColor(loot.Rarity));

            // Ausrüstungs-/Delta-Info
            string equipLine = equipped
                ? $"✓ Automatisch ausgerüstet{(replaced != null ? $" (ersetzt: {replaced.Name} +{replaced.Power})" : "")}"
                : "Ins Inventar gelegt";
            string deltaLine = !string.IsNullOrWhiteSpace(delta) ? $"Änderungen: {delta}" : "";

            int infoY = targetY + 2;
            WriteCentered(equipLine, innerLeft, infoY, innerW, equipped ? ConsoleColor.Green : ConsoleColor.Gray);
            if (!string.IsNullOrEmpty(deltaLine))
                WriteCentered(deltaLine, innerLeft, infoY + 1, innerW, ConsoleColor.DarkCyan);

            // Abschluss-Hinweis
            ConsoleUI.Write(innerLeft, y + h - 2, "[Enter] weiter");
            BeepSoft(); 

            while (ConsoleUI.ReadKey().Key != ConsoleKey.Enter) { }

            Console.CursorVisible = true;
        }

        // Helpers & Effekte

        private static (int frameDelay, int textDelay, int sparkleFrames) BaseTimingsFor(AnimSpeed speed)
        {
            double factor = speed switch
            {
                AnimSpeed.Slow => 1.5,
                AnimSpeed.Fast => 0.75,
                _ => 1.0
            };
            int frameDelay = (int)(220 * factor); // Chest-Frames
            int textDelay = (int)(90 * factor); // Fly-in
            int sparkleFrames = (int)(12 * factor); // Glitzer-Dauer
            return (frameDelay, textDelay, sparkleFrames);
        }

        private static ConsoleColor LootColor(DungeonXRaid.Items.Rarity r) => r switch
        {
            DungeonXRaid.Items.Rarity.Common => ConsoleColor.Gray,
            DungeonXRaid.Items.Rarity.Rare => ConsoleColor.Cyan,
            DungeonXRaid.Items.Rarity.Epic => ConsoleColor.Magenta,
            DungeonXRaid.Items.Rarity.Legendary => ConsoleColor.Yellow,
            _ => ConsoleColor.Gray
        };

        private static bool SleepSkippable(int ms)
        {
            // kleine Steps, damit Enter das Waiting unterbricht
            int step = Math.Clamp(ms / 10, 10, 30);
            int waited = 0;
            while (waited < ms)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                    return true;
                Thread.Sleep(step);
                waited += step;
            }
            return false;
        }

        private static void FlyInCentered(string text, int left, int top, int width, int startY, int targetY, int delay, ConsoleColor color)
        {
            int textWidth = Math.Min(text.Length, width);
            int textX = left + Math.Max(0, (width - textWidth) / 2);
            int prevY = -1;

            // Sicherheits-Clear: komplettes vertikales Band einmal leeren
            for (int y = Math.Min(targetY, startY); y <= Math.Max(targetY, startY); y++)
                ConsoleUI.Write(left, y, new string(' ', width));

            int steps = Math.Max(6, Math.Abs(startY - targetY));
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                // easeOutQuad
                double eased = 1 - (1 - t) * (1 - t);
                int yPos = targetY + (int)Math.Round((startY - targetY) * (1 - eased));

                // alte Position komplett löschen
                if (prevY >= 0)
                    ConsoleUI.Write(left, prevY, new string(' ', width));

                // neue Position schreiben
                WithColor(color, () =>
                {
                    string slice = text.Length > width ? text[..width] : text;
                    ConsoleUI.Write(textX, yPos, slice);
                });

                prevY = yPos;

                if (SleepSkippable(delay)) break;
            }

            
            for (int y = Math.Min(targetY, startY); y <= Math.Max(targetY, startY); y++)
                if (y != prevY)
                    ConsoleUI.Write(left, y, new string(' ', width));
        }

        private static void WriteCentered(string text, int left, int y, int width, ConsoleColor color)
        {
            int tx = left + Math.Max(0, (width - text.Length) / 2);
            WithColor(color, () => ConsoleUI.Write(tx, y, text.Length > width ? text[..width] : text));
        }

        private static void WithColor(ConsoleColor color, Action work)
        {
            var old = Console.ForegroundColor;
            try { Console.ForegroundColor = color; work(); }
            finally { Console.ForegroundColor = old; }
        }

        private static void Sparkle(int left, int top, int width, int height, int frames, int delay)
        {
            var rnd = new Random();
            for (int f = 0; f < frames; f++)
            {
                int stars = 10;
                var points = new List<(int x, int y, char c)>(stars);
                for (int i = 0; i < stars; i++)
                {
                    int sx = left + rnd.Next(Math.Max(1, width - 1));
                    int sy = top + rnd.Next(Math.Max(1, height));
                    char ch = rnd.Next(4) switch { 0 => '*', 1 => '·', 2 => '+', _ => '✦' };
                    points.Add((sx, sy, ch));
                }

                // zeichnen
                WithColor(ConsoleColor.Yellow, () =>
                {
                    foreach (var p in points) ConsoleUI.Write(p.x, p.y, p.c.ToString());
                });

                if (SleepSkippable(delay)) break;

                // wegradieren
                foreach (var p in points) ConsoleUI.Write(p.x, p.y, " ");
            }
        }

        private static void BeepSoft()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            try
            {
                Console.Beep(600, 40);
                Console.Beep(800, 60);
            }
            catch {  }
        }

        private static string ItemBonusShort(ItemModel it)
        {
            var b = it.Bonus;
            var parts = new List<string>();
            void add(string k, int v) { if (v != 0) parts.Add($"+{k}{v}"); }
            add("STR", b.STR); add("DEX", b.DEX); add("INT", b.INT);
            add("VIT", b.VIT); add("DEF", b.DEF); if (b.HPBonus != 0) parts.Add($"+HP{b.HPBonus}");
            return parts.Count == 0 ? "—" : string.Join(", ", parts);
        }

        private static List<string[]> ChestFrames() => new()
        {
            new [] // geschlossen
            {
                "            _________________            ",
                "           /                /|           ",
                "          /________________ / |          ",
                "          |   _________   |  |          ",
                "          |  |         |  |  |          ",
                "          |  |_________|  |  |          ",
                "          |________________| /           ",
            },
            new [] // Schloss klickt
            {
                "            _________________            ",
                "           /                /|           ",
                "          /________________ / |          ",
                "          |   ___ · ___   |  |           ",
                "          |  |   ( )   |  |  |           ",
                "          |  |_________|  |  |           ",
                "          |________________| /           ",
            },
            new [] // Deckel löst sich
            {
                "            _________________            ",
                "           /   _________  /|             ",
                "          /___/        /_/ |             ",
                "          |   ___   ___ |  |             ",
                "          |  |   ( )   ||  |             ",
                "          |  |_________||  |             ",
                "          |________________|/             ",
            },
            new [] // Deckel offen
            {
                "              _____________               ",
                "             /  *  *  *   \\              ",
                "            /______________ \\             ",
                "          _/______________/_/             ",
                "          |  |         |  |               ",
                "          |  |_________|  |               ",
                "          |________________|               ",
            },
            new [] // Glitzern
            {
                "              *  *  *  *                  ",
                "            *    ☆      *                 ",
                "              *  *  *  *                  ",
                "          _/______________/_/             ",
                "          |  |         |  |               ",
                "          |  |_________|  |               ",
                "          |________________|               ",
            }
        };
    }
}
