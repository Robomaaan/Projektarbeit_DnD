using DungeonXRaid.Core;

namespace DungeonXRaid.UI
{
    public static class MainMenu
    {
        public enum Result { Play, NewCharacter, Load, Delete, Quit }

        public static Result Show()
        {
            Console.CursorVisible = false;

            string title = "DungeonXRaid – Hauptmenü";
            string[] items = { "Spielen", "Charakter erstellen", "Spielstand laden", "Spielstand löschen", "Beenden" };
            int index = 0;
            const string mark = "> ";
            const string nomark = "  ";

            ConsoleUI.ClearWithSize();
            ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, title);
            int cx = 6, cy = 5;
            for (int i = 0; i < items.Length; i++)
                ConsoleUI.Write(cx, cy + i * 2, ((i == index) ? mark : nomark) + items[i]);
            ConsoleUI.Write(cx, cy + items.Length * 2 + 1, "[Enter] auswählen   [Esc] zurück/beenden");

            while (true)
            {
                var k = ConsoleUI.ReadKey().Key;

                if (k == ConsoleKey.UpArrow || k == ConsoleKey.DownArrow)
                {
                    int old = index;
                    index = (k == ConsoleKey.UpArrow) ? (index - 1 + items.Length) % items.Length
                                                      : (index + 1) % items.Length;
                    ConsoleUI.Write(cx, cy + old * 2, nomark + items[old] + new string(' ', 4));
                    ConsoleUI.Write(cx, cy + index * 2, mark + items[index]);
                }
                else if (k == ConsoleKey.Enter)
                {
                    Console.CursorVisible = false;
                    return index switch
                    {
                        0 => Result.Play,
                        1 => Result.NewCharacter,
                        2 => Result.Load,
                        3 => Result.Delete,
                        _ => Result.Quit
                    };
                }
                else if (k == ConsoleKey.Escape)
                {
                    Console.CursorVisible = false;
                    return Result.Quit;
                }
            }
        }
    }

    public static class CharacterCreator
    {
        private static (string line, ConsoleColor color) ClassLine(HeroClass c)
        {
            var proto = Character.New("Preview", c);
            string stats = $"STR {proto.Base.STR}  DEX {proto.Base.DEX}  INT {proto.Base.INT}  VIT {proto.Base.VIT}  DEF {proto.Base.DEF}";
            string name = c switch { HeroClass.Warrior => "Warrior", HeroClass.Mage => "Mage", HeroClass.Rogue => "Rogue", _ => "Monk" };
            var col = c switch { HeroClass.Warrior => ConsoleColor.Yellow, HeroClass.Mage => ConsoleColor.Cyan, HeroClass.Rogue => ConsoleColor.Green, _ => ConsoleColor.Magenta };
            return ($"{name,-8}  |  {stats}", col);
        }

        public static Character? Run()
        {
            Console.CursorVisible = false;
            ConsoleUI.ClearWithSize();
            ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, "Charakter erstellen");

            var name = ConsoleUI.Prompt(6, 6, "Name: ");
            if (string.IsNullOrWhiteSpace(name)) return null;

            int cx = 6; int cy = 9;
            ConsoleUI.Write(cx, cy, "Klasse wählen (↑/↓, Enter, Esc):");

            var classes = new[] { HeroClass.Warrior, HeroClass.Mage, HeroClass.Rogue, HeroClass.Monk };
            int idx = 0;

            void Paint()
            {
                for (int i = 0; i < classes.Length; i++)
                {
                    var (line, col) = ClassLine(classes[i]);
                    string marker = (i == idx) ? "> " : "  ";
                    ConsoleUI.WithColor(col, () => ConsoleUI.Write(cx, cy + 2 + i, marker + line + "    "));
                }
            }

            Paint();
            while (true)
            {
                var k = ConsoleUI.ReadKey().Key;
                if (k == ConsoleKey.Escape) return null;
                if (k == ConsoleKey.UpArrow) { idx = (idx - 1 + classes.Length) % classes.Length; Paint(); }
                else if (k == ConsoleKey.DownArrow) { idx = (idx + 1) % classes.Length; Paint(); }
                else if (k == ConsoleKey.Enter) break;
                else if (k is ConsoleKey.D1 or ConsoleKey.NumPad1) { idx = 0; Paint(); break; }
                else if (k is ConsoleKey.D2 or ConsoleKey.NumPad2) { idx = 1; Paint(); break; }
                else if (k is ConsoleKey.D3 or ConsoleKey.NumPad3) { idx = 2; Paint(); break; }
                else if (k is ConsoleKey.D4 or ConsoleKey.NumPad4) { idx = 3; Paint(); break; }
            }

            return Character.New(name, classes[idx]);
        }
    }

    public static class LoadMenu
    {
        public static GameSession? ShowAndLoad()
        {
            Console.CursorVisible = false;

            int index = 0;
            const string mark = "> ";
            const string nomark = "  ";

            while (true)
            {
                ConsoleUI.ClearWithSize();
                ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, "Spielstand laden");
                var saves = SaveSystem.ListSaves();

                int cx = 6, cy = 5;
                if (saves.Count == 0)
                {
                    ConsoleUI.Write(cx, cy, "Keine Spielstände vorhanden.");
                    ConsoleUI.Write(cx, cy + 2, "[Esc] zurück");
                    if (ConsoleUI.ReadKey().Key == ConsoleKey.Escape) return null;
                    continue;
                }

                index = Math.Clamp(index, 0, saves.Count - 1);

                for (int i = 0; i < saves.Count; i++)
                    ConsoleUI.Write(cx, cy + i, ((i == index) ? mark : nomark) + saves[i].Display);
                ConsoleUI.Write(cx, cy + saves.Count + 2, "[Enter] auswählen   [Esc] zurück");

                while (true)
                {
                    var k = ConsoleUI.ReadKey().Key;
                    if (k == ConsoleKey.Escape) return null;

                    if (k == ConsoleKey.UpArrow || k == ConsoleKey.DownArrow)
                    {
                        int old = index;
                        index = (k == ConsoleKey.UpArrow) ? (index - 1 + saves.Count) % saves.Count
                                                          : (index + 1) % saves.Count;
                        ConsoleUI.Write(cx, cy + old, nomark + saves[old].Display + new string(' ', 4));
                        ConsoleUI.Write(cx, cy + index, mark + saves[index].Display);
                    }
                    else if (k == ConsoleKey.Enter)
                    {
                        var s = SaveSystem.Load(saves[index].Id);
                        if (s != null) return s;
                    }
                    else break;
                }
            }
        }
    }

    public static class DeleteMenu
    {
        public static void ShowAndDelete()
        {
            Console.CursorVisible = false;

            int index = 0;
            const string mark = "> ";
            const string nomark = "  ";

            while (true)
            {
                ConsoleUI.ClearWithSize();
                ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, "Spielstand löschen");
                var saves = SaveSystem.ListSaves();

                int cx = 6, cy = 5;
                if (saves.Count == 0)
                {
                    ConsoleUI.Write(cx, cy, "Keine Spielstände vorhanden.");
                    ConsoleUI.Write(cx, cy + 2, "[Esc] zurück");
                    if (ConsoleUI.ReadKey().Key == ConsoleKey.Escape) return;
                    continue;
                }

                index = Math.Clamp(index, 0, saves.Count - 1);

                for (int i = 0; i < saves.Count; i++)
                    ConsoleUI.Write(cx, cy + i, ((i == index) ? mark : nomark) + saves[i].Display);
                ConsoleUI.Write(cx, cy + saves.Count + 2, "[Enter] löschen   [Esc] zurück");

                while (true)
                {
                    var k = ConsoleUI.ReadKey().Key;
                    if (k == ConsoleKey.Escape) return;

                    if (k == ConsoleKey.UpArrow || k == ConsoleKey.DownArrow)
                    {
                        int old = index;
                        index = (k == ConsoleKey.UpArrow) ? (index - 1 + saves.Count) % saves.Count
                                                          : (index + 1) % saves.Count;
                        ConsoleUI.Write(cx, cy + old, nomark + saves[old].Display + new string(' ', 4));
                        ConsoleUI.Write(cx, cy + index, mark + saves[index].Display);
                    }
                    else if (k == ConsoleKey.Enter)
                    {
                        var ok = SaveSystem.Delete(saves[index].Id);
                        MessageBox.ShowCenter(ok ? "Gelöscht." : "Löschen fehlgeschlagen.");
                        break; // Liste neu laden
                    }
                    else break;
                }
            }
        }
    }
}
