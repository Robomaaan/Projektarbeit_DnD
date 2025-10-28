using DungeonXRaid.Core;
using DungeonXRaid.UI;

namespace DungeonXRaid
{
    public static class App
    {
        public static void Run()
        {
            Console.Title = "DungeonXRaid – ASCII Rogue";
            Console.CursorVisible = false;

            GameSession? current = null;

            while (true)
            {
                var choice = MainMenu.Show();

                switch (choice)
                {
                    case MainMenu.Result.Play:
                        if (current == null)
                        {
                            MessageBox.ShowCenter("Kein aktiver Spielstand.\n\nErstelle zuerst einen Charakter oder lade einen Spielstand.");
                            break;
                        }

                        // Spiel (Run)
                        DungeonGame.Run(current);
                        current.LastPlayed = DateTime.Now;

                        // Gold in Meta-Bank übertragen (Run-Ende)
                        var metaEnd = MetaStore.Load();
                        if (current.Hero.Gold > 0)
                        {
                            metaEnd.BankGold += current.Hero.Gold;
                            current.Hero.Gold = 0;
                            MetaStore.Save(metaEnd);
                            MessageBox.ShowCenter($"Run beendet.\nDein Gold wurde in die Meta-Bank übertragen.\nBank: {metaEnd.BankGold}");
                        }

                        // Wenn tot -> Shop anbieten und neuer Charakter (mit Upgrades) möglich
                        if (current.Hero.Hp <= 0)
                        {
                            ShowMetaShop(metaEnd);

                            var again = CharacterCreator.Run();
                            if (again != null)
                            {
                                MetaStore.ApplyTo(again, metaEnd);
                                current = GameSession.New(again);
                                SaveSystem.Save(current);
                                MessageBox.ShowCenter("Neuer Run gestartet. Viel Erfolg!");
                            }
                        }
                        break;

                    case MainMenu.Result.NewCharacter:
                        {
                            var hero = CharacterCreator.Run();
                            if (hero != null)
                            {
                                // Metaprogression anwenden
                                var meta = MetaStore.Load();
                                MetaStore.ApplyTo(hero, meta);

                                current = GameSession.New(hero);
                                SaveSystem.Save(current);

                                MessageBox.ShowCenter($"Charakter erstellt:\n• {hero.Name} ({hero.Class})\nMeta-Upgrades aktiv.");
                            }
                            break;
                        }

                    case MainMenu.Result.Load:
                        {
                            var loaded = LoadMenu.ShowAndLoad();
                            if (loaded != null)
                            {
                                var meta = MetaStore.Load();
                                MetaStore.ApplyTo(loaded.Hero, meta);

                                current = loaded;
                                MessageBox.ShowCenter($"Spielstand geladen: {current.SaveName}\nMeta-Upgrades aktiv.");
                            }
                            break;
                        }

                    case MainMenu.Result.Delete:
                        DeleteMenu.ShowAndDelete();
                        break;

                    case MainMenu.Result.Quit:
                        return;
                }
            }
        }

        // sehr einfacher Text-Shop (Bankgold ausgeben)
        private static void ShowMetaShop(MetaProgress meta)
        {
            while (true)
            {
                ConsoleUI.ClearWithSize();
                ConsoleUI.DrawBox(2, 1, Console.WindowWidth - 4, Console.WindowHeight - 2, "Meta-Shop (Bankgold ausgeben)");
                int x = 6, y = 5;

                ConsoleUI.Write(x, y, $"Bankgold: {meta.BankGold}");
                ConsoleUI.Write(x, y + 2, $"[1] STR (+1)  Kosten: {meta.CostFor("STR")}   Aktuell: {meta.STR}");
                ConsoleUI.Write(x, y + 3, $"[2] DEX (+1)  Kosten: {meta.CostFor("DEX")}   Aktuell: {meta.DEX}");
                ConsoleUI.Write(x, y + 4, $"[3] INT (+1)  Kosten: {meta.CostFor("INT")}   Aktuell: {meta.INT}");
                ConsoleUI.Write(x, y + 5, $"[4] VIT (+1)  Kosten: {meta.CostFor("VIT")}   Aktuell: {meta.VIT}");
                ConsoleUI.Write(x, y + 6, $"[5] DEF (+1)  Kosten: {meta.CostFor("DEF")}   Aktuell: {meta.DEF}");
                ConsoleUI.Write(x, y + 8, "[Enter] weiter  |  [Esc] zurück");

                var k = ConsoleUI.ReadKey().Key;
                if (k == ConsoleKey.Escape || k == ConsoleKey.Enter) { MetaStore.Save(meta); return; }

                void TryBuy(string stat, Action inc)
                {
                    int cost = meta.CostFor(stat);
                    if (meta.BankGold >= cost) { meta.BankGold -= cost; inc(); MetaStore.Save(meta); }
                    else MessageBox.ShowCenter("Nicht genug Bankgold.");
                }

                if (k == ConsoleKey.D1 || k == ConsoleKey.NumPad1) TryBuy("STR", () => meta.STR++);
                else if (k == ConsoleKey.D2 || k == ConsoleKey.NumPad2) TryBuy("DEX", () => meta.DEX++);
                else if (k == ConsoleKey.D3 || k == ConsoleKey.NumPad3) TryBuy("INT", () => meta.INT++);
                else if (k == ConsoleKey.D4 || k == ConsoleKey.NumPad4) TryBuy("VIT", () => meta.VIT++);
                else if (k == ConsoleKey.D5 || k == ConsoleKey.NumPad5) TryBuy("DEF", () => meta.DEF++);
            }
        }
    }
}
