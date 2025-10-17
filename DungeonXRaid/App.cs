using System;
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
                // Hauptmenü anzeigen (Pfeiltasten/Enter/Esc handled in MainMenu.Show)
                var choice = MainMenu.Show();

                switch (choice)
                {
                    case MainMenu.Result.Play:
                        if (current == null)
                        {
                            MessageBox.ShowCenter(
                                "Kein aktiver Spielstand.\n\n" +
                                "Erstelle zuerst einen Charakter oder lade einen Spielstand."
                            );
                            break;
                        }
                        DungeonGame.Run(current); // Spiel starten
                        current.LastPlayed = DateTime.Now;
                        break;

                    case MainMenu.Result.NewCharacter:
                        {
                            var hero = CharacterCreator.Run();
                            if (hero != null)
                            {
                                current = GameSession.New(hero);
                                SaveSystem.Save(current);
                                MessageBox.ShowCenter(
                                    $"Charakter erstellt:\n\n• {hero.Name} ({hero.Class})\n\n" +
                                    "Neuer Spielstand gespeichert."
                                );
                            }
                            break;
                        }

                    case MainMenu.Result.Load:
                        {
                            var loaded = LoadMenu.ShowAndLoad();
                            if (loaded != null)
                            {
                                current = loaded;
                                MessageBox.ShowCenter($"Spielstand geladen: {current.SaveName}");
                            }
                            break;
                        }

                    case MainMenu.Result.Delete:
                        DeleteMenu.ShowAndDelete();
                        // (Optional) Du kannst hier current auf null setzen, wenn du sicher bist, dass der
                        // aktive Spielstand gelöscht wurde. Ohne Rückgabe-ID lassen wir ihn bestehen.
                        break;

                    case MainMenu.Result.Quit:
                        // zurück an Program.Main -> Environment.Exit erfolgt dort
                        return;
                }
            }
        }
    }
}
