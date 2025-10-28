using System.Text;
using DungeonXRaid.UI;

namespace DungeonXRaid
{
    internal static class Program
    {
        static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            // Einmalig Konsole initialisieren (Buffer/Window nur beim Start setzen)
            ConsoleUI.InitConsole(110, 38);

            try
            {
                Console.Title = "DungeonXRaid – ASCII Rogue";
                Console.CursorVisible = false;
                App.Run();
            }
            finally
            {
                Console.CursorVisible = true;
            }

            Environment.Exit(0);
        }
    }
}