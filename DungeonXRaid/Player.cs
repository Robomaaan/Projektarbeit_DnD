namespace DungeonXRaid
{
    public class Player : Entity
    {
        public Player(int x, int y) : base(x, y, '@') { }

        public void TryMove(int dx, int dy, Map map)
        {
            int nx = X + dx, ny = Y + dy;
            
            if (map.IsWalkable(nx, ny))
            {
                X = nx;
                Y = ny;
            }
        }
    }
}
