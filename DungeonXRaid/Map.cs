namespace DungeonXRaid
{
    using DungeonXRaid.Core;
    using DungeonXRaid.Items;

    public class Map
    {
        public int Width { get; }
        public int Height { get; }
        private readonly char[,] tiles;
        private readonly Random rng = new();

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new char[Height, Width];
            Generate();
        }

        public char GetTile(int x, int y) => tiles[y, x];

        // Walkable: alles außer Wand ('#') – so kann man auch auf Truhen stehen
        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            return tiles[y, x] != '#';
        }

        public bool IsChest(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            return tiles[y, x] == 'C';
        }

        // Öffnet eine Truhe (falls vorhanden), ersetzt Feld durch Boden und gibt Loot zurück.
        public bool TryOpenChest(int x, int y, out ItemModel loot)
        {
            loot = default!;
            if (!IsChest(x, y)) return false;
            loot = LootTable.Roll(rng);
            tiles[y, x] = '.'; // Truhe „verbraucht“
            return true;
        }

        public (int x, int y) GetRandomFloor()
        {
            for (int i = 0; i < 2000; i++)
            {
                int x = rng.Next(1, Width - 1);
                int y = rng.Next(1, Height - 1);
                if (IsWalkable(x, y) && !IsChest(x, y)) return (x, y);
            }
            return (1, 1);
        }

        private void Generate()
        {
            Fill('#');
            var rooms = new List<(int cx, int cy)>();

            int area = Width * Height;
            int roomCount = Math.Clamp(area / 180, 10, 22); // viele kleine Räume

            for (int i = 0; i < roomCount; i++)
            {
                int w = rng.Next(5, 10);
                int h = rng.Next(4, 8);
                int x = rng.Next(1, Width - w - 1);
                int y = rng.Next(1, Height - h - 1);
                CarveRoom(x, y, w, h);

                int cx = x + w / 2;
                int cy = y + h / 2;
                rooms.Add((cx, cy));

                if (i > 0)
                {
                    var p = rooms[i - 1];
                    if (rng.Next(2) == 0)
                    {
                        CarveHCorridor(p.cx, cx, p.cy);
                        CarveVCorridor(p.cy, cy, cx);
                    }
                    else
                    {
                        CarveVCorridor(p.cy, cy, p.cx);
                        CarveHCorridor(p.cx, cx, cy);
                    }
                }
            }

            // Nach Raumgenerierung: zufällige Truhen setzen
            PlaceChests();
        }

        private void PlaceChests()
        {
            int area = Width * Height;
            int chestCount = Math.Clamp(area / 900, 3, 12); // 80x28 → ca. 6–7 Truhen

            int tries = chestCount * 20;
            while (chestCount > 0 && tries-- > 0)
            {
                int x = rng.Next(2, Width - 2);
                int y = rng.Next(2, Height - 2);

                if (tiles[y, x] == '.' && // auf Boden
                    tiles[y - 1, x] != '#' && tiles[y + 1, x] != '#' &&   // nicht direkt im schmalen Gang
                    tiles[y, x - 1] != '#' && tiles[y, x + 1] != '#' &&
                    tiles[y, x] != 'C')
                {
                    tiles[y, x] = 'C';
                    chestCount--;
                }
            }
        }

        private void Fill(char c)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    tiles[y, x] = c;
        }

        private void CarveRoom(int x, int y, int w, int h)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (xx > 0 && yy > 0 && xx < Width - 1 && yy < Height - 1)
                        tiles[yy, xx] = '.';
        }

        private void CarveHCorridor(int x1, int x2, int y)
        {
            int a = Math.Min(x1, x2), b = Math.Max(x1, x2);
            for (int x = a; x <= b; x++)
                if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
                    tiles[y, x] = '.';
        }

        private void CarveVCorridor(int y1, int y2, int x)
        {
            int a = Math.Min(y1, y2), b = Math.Max(y1, y2);
            for (int y = a; y <= b; y++)
                if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
                    tiles[y, x] = '.';
        }
    }
}
