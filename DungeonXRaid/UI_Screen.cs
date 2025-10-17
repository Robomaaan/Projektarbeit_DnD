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

            // einmaliger Rahmen
            ConsoleUI.ClearWithSize();
            ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, title);
            int cx = 6, cy = 5;
            for (int i = 0; i < items.Length; i++)
                ConsoleUI.Write(cx, cy + i * 2, ((i == index) ? mark : nomark) + items[i]);
            ConsoleUI.Write(cx, cy + items.Length * 2 + 1, "[Enter] auswählen   [Esc] zurück/beenden");

            // nur Auswahlzeilen aktualisieren
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
        public static Character? Run()
        {
            Console.CursorVisible = false;
            ConsoleUI.ClearWithSize();
            ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, "Charakter erstellen");

            var name = ConsoleUI.Prompt(6, 6, "Name: ");
            if (string.IsNullOrWhiteSpace(name)) return null;

            ConsoleUI.Write(6, 9, "Klasse wählen:  [1] Warrior  [2] Mage  [3] Rogue  [4] Monk");
            HeroClass cls = HeroClass.Rogue;
            while (true)
            {
                var k = ConsoleUI.ReadKey().Key;
                if (k == ConsoleKey.D1 || k == ConsoleKey.NumPad1) { cls = HeroClass.Warrior; break; }
                if (k == ConsoleKey.D2 || k == ConsoleKey.NumPad2) { cls = HeroClass.Mage; break; }
                if (k == ConsoleKey.D3 || k == ConsoleKey.NumPad3) { cls = HeroClass.Rogue; break; }
                if (k == ConsoleKey.D4 || k == ConsoleKey.NumPad4) { cls = HeroClass.Monk; break; }
                if (k == ConsoleKey.Escape) return null;
            }

            // Charakter mit Klassen-Startwerten erzeugen
            var hero = Character.New(name, cls);
            return hero;
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
                    var k0 = ConsoleUI.ReadKey().Key;
                    if (k0 == ConsoleKey.Escape) return null;
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
                    var k0 = ConsoleUI.ReadKey().Key;
                    if (k0 == ConsoleKey.Escape) return;
                    continue;
                }

                index = Math.Clamp(index, 0, saves.Count - 1);

                for (int i = 0; i < saves.Count; i++)
                    ConsoleUI.Write(cx, cy + i, ((i == index) ? mark : nomark) + saves[i].Display);
                ConsoleUI.Write(cx, cy + saves.Count + 2, "[Enter] auswählen   [Esc] zurück");

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
                        MessageBox.ShowCenter(ok ? "Spielstand gelöscht." : "Löschen fehlgeschlagen.");
                        break; 
                    }
                    else break;
                }
            }
        }
    }
}
