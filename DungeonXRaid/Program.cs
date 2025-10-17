namespace DungeonXRaid
{
    internal static class Program
    {
        static void Main()
        {
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
