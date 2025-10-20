namespace DungeonXRaid
{
    public class Enemy : Entity
    {
        public string Name { get; }
        public int Level { get; }
        public int Hp { get; private set; }
        public int Attack { get; }
        public int Defense { get; }
        public bool IsBoss { get; }

        public Enemy(string name, int x, int y, int level, int hp, int atk, int def, bool boss = false)
            : base(x, y, boss ? 'B' : 'E')
        {
            Name = name;
            Level = level;
            Hp = hp;
            Attack = atk;
            Defense = def;
            IsBoss = boss;
        }

        public bool TakeDamage(int dmg)
        {
            Hp -= Math.Max(1, dmg - Defense);
            return Hp <= 0;
        }
    }
}
