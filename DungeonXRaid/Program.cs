namespace DungeonXRaid
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "DungeonXRaid – ASCII Roguelike";
            var game = new Game(60, 20);
            game.Run();
        }
    }
}
