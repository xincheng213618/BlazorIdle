using System.Linq;
using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 9: Test that special attack applies buff to player
    /// </summary>
    public class Phase9SpecialBuffTest
    {
        /// <summary>
        /// Test that when player uses special attack, they receive the warrior test buff
        /// </summary>
        [Fact]
        public void SpecialAttack_AppliesTestBuffToPlayer()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var playerTeam = new BattleTeam<Character>("team_player", "Player Team", TeamType.Player);
            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 50,
                AttackRateAPS = 1.0,
                CritChancePercent = 0.0,
                CritMultiplier = 2.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 1.0, // Fast special for testing
                SpecialDamage = 100
            };
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("team_enemy", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy
            {
                MaxHp = 500,
                Hp = 500,
                DamagePerHit = 20,
                AttackIntervalSec = 2.0,
                VariancePct = 0.0
            };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            var config = new MultiBattleConfig
            {
                PlayerTargetStrategy = TargetStrategy.Random,
                EnemyTargetStrategy = TargetStrategy.Random,
                SpecialIsAoe = false // Single target to make it easier to test
            };

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config);
            battle.Start();

            // Act - advance enough ticks to trigger special attack
            for (int i = 0; i < 15; i++)
            {
                battle.AdvanceTick(100); // 100ms per tick, special triggers at 1000ms
            }

            // Assert - check that player has buffs
            var buffs = battle.GetPlayerBuffs("player1");
            
            Assert.NotNull(buffs);
            Assert.NotEmpty(buffs);
            
            // Phase 9: Updated to check for new buff ID (warrior_power_boost)
            // Look for the warrior power boost buff
            var warriorBuff = buffs.FirstOrDefault(b => b.Id == "warrior_power_boost");
            Assert.NotNull(warriorBuff);
            Assert.Equal(BuffKind.Buff, warriorBuff.Kind);
            Assert.Equal(BuffStackingPolicy.Refresh, warriorBuff.StackingPolicy);
            
            // Check that it has the expected effects
            Assert.NotNull(warriorBuff.Effects);
            Assert.Equal(3, warriorBuff.Effects.Count);
            
            // Verify effects exist (order doesn't matter) - using new attribute names
            Assert.Contains(warriorBuff.Effects, e => e.Type == BuffEffectType.StatMultiplier && e.Target == "AttackFinal");
            Assert.Contains(warriorBuff.Effects, e => e.Type == BuffEffectType.StatAdditive && e.Target == "HastePercent");
            Assert.Contains(warriorBuff.Effects, e => e.Type == BuffEffectType.StatAdditive && e.Target == "CritChancePercent");
            
            // Verify duration is still active
            Assert.True(warriorBuff.RemainingDurationSec > 0);
            
            // Phase 9: Verify additional buffs are applied (HoT, instant heal, etc.)
            // Check for regeneration buff (Phase 3: updated to regeneration_hot)
            var regenBuff = buffs.FirstOrDefault(b => b.Id == "regeneration_hot");
            Assert.NotNull(regenBuff);
            Assert.Equal(BuffKind.Buff, regenBuff.Kind);
            
            // Check for instant heal buff (might have expired quickly)
            // Note: instant heal has very short duration, so it might not be present
            var instantHealBuff = buffs.FirstOrDefault(b => b.Id == "instant_heal");
            // Don't assert on instant heal as it expires very quickly
        }

    }
}
