namespace DungeonXRaid
{
    public class Player : Entity
    {
        public Player(int x, int y) : base(x, y, '@') { }

        public void TryMove(int dx, int dy, char[,] map)
        {
            int nx = X + dx, ny = Y + dy;
            if (Map.IsWalkable(map, nx, ny))
            {
                X = nx; Y = ny;
            }
        }
    }
}
