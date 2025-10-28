using System.Text;

namespace DungeonXRaid
{
    internal static class Program
    {
        static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
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
//Dokumentation geschrieben.