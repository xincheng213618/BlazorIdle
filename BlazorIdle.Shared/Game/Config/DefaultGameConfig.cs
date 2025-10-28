using System.Collections.Generic;

namespace BlazorIdle.Game.Config
{
    public static class DefaultGameConfig
    {
        public static List<ProfessionDef> DefaultProfessions() => new()
        {
            new ProfessionDef
            {
                Id = "warrior", Name = "Warrior", Desc="Fallback",
                MaxHp = 280, AttackRateAPS = 1.8, DamagePerAttack = 18,
                HastePercent = 0, SpecialIntervalSec = 6, SpecialDamage = 140,
                CritChancePercent = 10, CritMultiplier = 1.5, VariancePct = 0.04,
                ReviveSec = 5.0
            }
        };

        public static List<MonsterDef> DefaultMonsters() => new()
        {
            new MonsterDef
            {
                Id = "slime", Name = "Green Slime", Desc="Fallback", Level=1,
                MaxHp = 220, AttackIntervalSec = 2.0, DamagePerHit = 8,
                VariancePct = 0.05, RespawnSec = 2.5
            }
        };
    }
}