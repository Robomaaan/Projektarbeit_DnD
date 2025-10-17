namespace DungeonXRaid.UI
{
    using System.Runtime.InteropServices;

    public static class ConsoleUI
    {
        // --------- Low-level Safe Writes ---------
        private static int MaxW => Math.Max(1, Math.Min(Console.BufferWidth, Console.WindowWidth));
        private static int MaxH => Math.Max(1, Math.Min(Console.BufferHeight, Console.WindowHeight));

        private static void SafeSetCursor(int x, int y)
        {
            if (x < 0 || y < 0) return;
            if (y >= MaxH) return;
            if (x >= MaxW) return;
            Console.SetCursorPosition(x, y);
        }

        public static void Put(int x, int y, char c)
        {
            if (x < 0 || y < 0 || y >= MaxH || x >= MaxW) return;
            SafeSetCursor(x, y);
            Console.Write(c);
        }

        public static void Write(int x, int y, string text)
        {
            if (y < 0 || y >= MaxH) return;
            if (x < 0)
            { // links abschneiden
                int cut = -x;
                if (cut >= text.Length) return;
                text = text.Substring(cut);
                x = 0;
            }
            if (x >= MaxW) return;
            int room = MaxW - x;
            if (text.Length > room) text = text.Substring(0, room);
            SafeSetCursor(x, y);
            Console.Write(text);
        }

        public static ConsoleKeyInfo ReadKey() => Console.ReadKey(true);

        // --------- High-level UI helpers ---------
        public static void ClearWithSize(int w = 110, int h = 35)
        {
            // Zielgrößen begrenzen
            var W = Math.Min(w, Console.LargestWindowWidth);
            var H = Math.Min(h, Console.LargestWindowHeight);

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Reihenfolge: erst Buffer >= Window, dann Window
                    int bw = Math.Max(W, Console.BufferWidth);
                    int bh = Math.Max(H, Console.BufferHeight);
                    Console.SetBufferSize(bw, bh);
                    Console.SetWindowSize(Math.Min(W, bw), Math.Min(H, bh));
                }
            }
            catch
            {
                // Auf Nicht-Windows/kleinen Terminals: best effort weiter
            }

            Console.Clear();
        }

        public static void DrawBox(int x, int y, int w, int h, string title = "")
        {
            // Clipping auf sichtbare Fläche
            int maxW = MaxW, maxH = MaxH;
            if (x >= maxW || y >= maxH) return;
            w = Math.Max(2, Math.Min(w, maxW - x));
            h = Math.Max(2, Math.Min(h, maxH - y));

            // Ober-/Unterkante
            for (int i = 0; i < w; i++)
            {
                Put(x + i, y, i == 0 ? '┌' : (i == w - 1 ? '┐' : '─'));
                Put(x + i, y + h - 1, i == 0 ? '└' : (i == w - 1 ? '┘' : '─'));
            }
            // Seiten
            for (int j = 1; j < h - 1; j++)
            {
                Put(x, y + j, '│');
                Put(x + w - 1, y + j, '│');
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Titel auf Innenbreite kürzen
                int inner = Math.Max(0, w - 4);
                string t = $" {title} ";
                if (t.Length > inner) t = t.Substring(0, inner);
                Write(x + 2, y, t);
            }
        }

        public static string Prompt(int x, int y, string label)
        {
            Write(x, y, label);
            Console.CursorVisible = true;
            var input = Console.ReadLine() ?? "";
            Console.CursorVisible = false;
            return input.Trim();
        }
    }

    public static class MessageBox
    {
        public static void ShowCenter(string text)
        {
            ConsoleUI.ClearWithSize();
            Console.CursorVisible = false;

            int w = Math.Clamp(70, 30, Math.Max(10, Console.WindowWidth - 4));
            int h = 8;

            int x = Math.Max(0, (Console.WindowWidth - w) / 2);
            int y = Math.Max(0, (Console.WindowHeight - h) / 2);

            ConsoleUI.DrawBox(x, y, w, h, "Info");

            // Word-Wrap innerhalb der Box
            int innerW = w - 4;
            var lines = new List<string>();
            foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
            {
                var t = raw.TrimEnd();
                while (t.Length > innerW)
                {
                    lines.Add(t.Substring(0, innerW));
                    t = t.Substring(innerW);
                }
                lines.Add(t);
            }

            int startY = y + 2;
            int maxLines = Math.Max(1, h - 4);
            for (int i = 0; i < Math.Min(maxLines, lines.Count); i++)
                ConsoleUI.Write(x + 2, startY + i, lines[i]);

            ConsoleUI.Write(x + 2, y + h - 2, "Drücke [Enter] …");
            while (ConsoleUI.ReadKey().Key != ConsoleKey.Enter) { }

            Console.CursorVisible = true;
        }
    }
}
