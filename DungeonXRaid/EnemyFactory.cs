namespace DungeonXRaid
{
    public static class EnemyFactory
    {
        public static List<Enemy> GetEnemiesForStage(int stage)
        {
            var list = new List<Enemy>();
            switch (stage)
            {
                case 1:
                    list.Add(new Enemy("Kleiner Goblin", 0, 0, 1, 15, 4, 1));
                    list.Add(new Enemy("Riesenratte", 0, 0, 1, 10, 3, 0));
                    list.Add(new Enemy("Fledermaus", 0, 0, 1, 8, 5, 0));
                    break;
                case 2:
                    list.Add(new Enemy("Ork", 0, 0, 2, 25, 6, 2));
                    list.Add(new Enemy("Skelettkrieger", 0, 0, 2, 20, 7, 1));
                    list.Add(new Enemy("Nachtschatten", 0, 0, 2, 18, 8, 1));
                    break;
                case 3:
                    list.Add(new Enemy("Dämon", 0, 0, 3, 40, 10, 3));
                    list.Add(new Enemy("Todesritter", 0, 0, 3, 45, 12, 4));
                    list.Add(new Enemy("Feuergeist", 0, 0, 3, 30, 11, 2));
                    break;
            }
            return list;
        }

        public static Enemy CreateBoss(int stage)
        {
            return stage switch
            {
                1 => new Enemy("Goblinkönig", 0, 0, 3, 50, 8, 3, true),
                2 => new Enemy("Nachtfürst", 0, 0, 5, 80, 10, 4, true),
                3 => new Enemy("Dämonenfürst Azrak", 0, 0, 8, 120, 15, 6, true),
                _ => new Enemy("Unbekannter Albtraum", 0, 0, 10, 150, 20, 8, true)
            };
        }
    }
}
