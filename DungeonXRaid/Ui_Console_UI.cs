using System;

namespace DungeonXRaid.UI
{
    public static class ConsoleUI
    {
        public static void ClearWithSize(int w = 110, int h = 38)
        {
            var W = Math.Min(w, Console.LargestWindowWidth);
            var H = Math.Min(h, Console.LargestWindowHeight);
            Console.SetWindowSize(W, H);
            Console.SetBufferSize(W, H);
            Console.Clear();
        }

        public static void DrawBox(int x, int y, int w, int h, string title = "")
        {
            for (int i = 0; i < w; i++)
            {
                Put(x + i, y, i == 0 ? '┌' : (i == w - 1 ? '┐' : '─'));
                Put(x + i, y + h - 1, i == 0 ? '└' : (i == w - 1 ? '┘' : '─'));
            }
            for (int j = 1; j < h - 1; j++)
            {
                Put(x, y + j, '│');
                Put(x + w - 1, y + j, '│');
            }
            if (!string.IsNullOrWhiteSpace(title))
            {
                Write(x + 2, y, $" {title} ");
            }
        }

        public static void Write(int x, int y, string text)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(text);
        }

        public static void Put(int x, int y, char c)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(c);
        }

        public static string Prompt(int x, int y, string label)
        {
            Write(x, y, label);
            Console.CursorVisible = true;
            var input = Console.ReadLine() ?? "";
            Console.CursorVisible = false;
            return input.Trim();
        }

        public static ConsoleKeyInfo ReadKey() => Console.ReadKey(true);
    }

    public static class MessageBox
    {
        public static void ShowCenter(string text)
        {
            ConsoleUI.ClearWithSize();
            var w = 70; var h = 8;
            var x = (Console.WindowWidth - w) / 2;
            var y = (Console.WindowHeight - h) / 2;
            ConsoleUI.DrawBox(x, y, w, h, "Info");
            ConsoleUI.Write(x + 2, y + 2, text);
            ConsoleUI.Write(x + 2, y + h - 2, "Drücke [Enter] …");
            while (ConsoleUI.ReadKey().Key != ConsoleKey.Enter) { }
        }
    }
}
