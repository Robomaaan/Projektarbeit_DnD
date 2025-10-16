namespace DungeonXRaid
{
    public abstract class Entity
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public char Glyph { get; protected set; }

        protected Entity(int x, int y, char glyph)
        {
            X = x; Y = y; Glyph = glyph;
        }
    }
}
