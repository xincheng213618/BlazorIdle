using System;
using System.Collections.Generic;
using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for IBuffOwner implementations (Phase 4).
    /// </summary>
    public class BuffOwnerTests
    {
        #region CharacterBuffOwner Tests

        [Fact]
        public void CharacterBuffOwner_Constructor_SetsPropertiesCorrectly()
        {
            var character = new Character { Hp = 100, MaxHp = 200 };
            var resources = new ResourceBucketCollection();
            
            var owner = new CharacterBuffOwner(character, "test_char_id", resources);

            Assert.True(owner.IsPlayer);
            Assert.Equal(100, owner.CurrentHp);
            Assert.Equal(200, owner.MaxHp);
            Assert.Same(resources, owner.Buckets);
            Assert.Empty(owner.Buffs);
        }

        [Fact]
        public void CharacterBuffOwner_ApplyBuff_AddsNewBuff()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff = new BuffInstance(
                "test_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>()
            );

            owner.ApplyBuff(buff);

            Assert.Single(owner.Buffs);
            Assert.True(owner.Buffs.ContainsKey("test_buff"));
        }

        [Fact]
        public void CharacterBuffOwner_ApplyBuff_RefreshPolicy_RefreshesDuration()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff1 = new BuffInstance(
                "refresh_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 5.0
            );

            owner.ApplyBuff(buff1);
            buff1.Tick(2.0); // Reduce to 3.0s
            Assert.Equal(3.0, buff1.RemainingDurationSec);

            var buff2 = new BuffInstance(
                "refresh_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 5.0
            );

            owner.ApplyBuff(buff2);
            Assert.Equal(5.0, buff1.RemainingDurationSec); // Duration refreshed
            Assert.Equal(1, buff1.Stacks); // No stack increase
        }

        [Fact]
        public void CharacterBuffOwner_ApplyBuff_StackPolicy_IncreasesStacks()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff1 = new BuffInstance(
                "stack_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 3
            );

            owner.ApplyBuff(buff1);
            Assert.Equal(1, buff1.Stacks);

            var buff2 = new BuffInstance(
                "stack_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 3
            );

            owner.ApplyBuff(buff2);
            Assert.Equal(2, buff1.Stacks); // Stack increased
        }

        [Fact]
        public void CharacterBuffOwner_ApplyBuff_IgnorePolicy_DoesNothing()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff1 = new BuffInstance(
                "ignore_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Ignore,
                durationSec: 3.0
            );

            owner.ApplyBuff(buff1);
            buff1.Tick(1.0); // Reduce to 2.0s
            
            var buff2 = new BuffInstance(
                "ignore_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Ignore,
                durationSec: 5.0
            );

            owner.ApplyBuff(buff2);
            Assert.Equal(2.0, buff1.RemainingDurationSec); // No change
            Assert.Equal(1, buff1.Stacks); // No stack increase
        }

        [Fact]
        public void CharacterBuffOwner_RemoveBuff_RemovesExistingBuff()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff = new BuffInstance(
                "test_buff",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect>()
            );

            owner.ApplyBuff(buff);
            Assert.Single(owner.Buffs);

            bool removed = owner.RemoveBuff("test_buff", "test");
            Assert.True(removed);
            Assert.Empty(owner.Buffs);
        }

        [Fact]
        public void CharacterBuffOwner_RemoveBuff_ReturnsFalseWhenNotFound()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");

            bool removed = owner.RemoveBuff("nonexistent", "test");
            Assert.False(removed);
        }

        [Fact]
        public void CharacterBuffOwner_ReceiveDamage_ReducesHp()
        {
            var character = new Character { Hp = 100, MaxHp = 200 };
            var owner = new CharacterBuffOwner(character, "test_char_id");

            owner.ReceiveDamage(30, new DamageMeta("test"));

            Assert.Equal(70, character.Hp);
            Assert.Equal(70, owner.CurrentHp);
        }

        [Fact]
        public void CharacterBuffOwner_ReceiveDamage_ClampsToZero()
        {
            var character = new Character { Hp = 50, MaxHp = 200 };
            var owner = new CharacterBuffOwner(character, "test_char_id");

            owner.ReceiveDamage(100, new DamageMeta("test"));

            Assert.Equal(0, character.Hp);
        }

        [Fact]
        public void CharacterBuffOwner_ReceiveDamage_CallsCallback()
        {
            var character = new Character { Hp = 100, MaxHp = 200 };
            int damageReceived = 0;
            DamageMeta? metaReceived = null;

            var owner = new CharacterBuffOwner(character, "test_char_id", null, (amount, meta) =>
            {
                damageReceived = amount;
                metaReceived = meta;
            });

            var damageMeta = new DamageMeta("dot_tick", "test_buff");
            owner.ReceiveDamage(25, damageMeta);

            Assert.Equal(25, damageReceived);
            Assert.Same(damageMeta, metaReceived);
        }

        [Fact]
        public void CharacterBuffOwner_ReceiveHeal_IncreasesHp()
        {
            var character = new Character { Hp = 50, MaxHp = 200 };
            var owner = new CharacterBuffOwner(character, "test_char_id");

            owner.ReceiveHeal(30, new HealMeta("test"));

            Assert.Equal(80, character.Hp);
            Assert.Equal(80, owner.CurrentHp);
        }

        [Fact]
        public void CharacterBuffOwner_ReceiveHeal_ClampsToMax()
        {
            var character = new Character { Hp = 180, MaxHp = 200 };
            var owner = new CharacterBuffOwner(character, "test_char_id");

            owner.ReceiveHeal(50, new HealMeta("test"));

            Assert.Equal(200, character.Hp);
        }

        [Fact]
        public void CharacterBuffOwner_ReceiveHeal_CallsCallback()
        {
            var character = new Character { Hp = 100, MaxHp = 200 };
            int healReceived = 0;
            HealMeta? metaReceived = null;

            var owner = new CharacterBuffOwner(character, "test_char_id", null, null, (amount, meta) =>
            {
                healReceived = amount;
                metaReceived = meta;
            });

            var healMeta = new HealMeta("hot_tick", "test_buff");
            owner.ReceiveHeal(40, healMeta);

            Assert.Equal(40, healReceived);
            Assert.Same(healMeta, metaReceived);
        }

        #endregion

        #region EnemyBuffOwner Tests

        [Fact]
        public void EnemyBuffOwner_Constructor_SetsPropertiesCorrectly()
        {
            var enemy = new Enemy { Hp = 150, MaxHp = 300 };
            var owner = new EnemyBuffOwner(enemy, "enemy_1");

            Assert.False(owner.IsPlayer);
            Assert.Equal("enemy_1", owner.Id);
            Assert.Equal(150, owner.CurrentHp);
            Assert.Equal(300, owner.MaxHp);
            Assert.Null(owner.Buckets); // Enemies don't have resources
            Assert.Empty(owner.Buffs);
        }

        [Fact]
        public void EnemyBuffOwner_ApplyBuff_AddsNewBuff()
        {
            var enemy = new Enemy();
            var owner = new EnemyBuffOwner(enemy, "enemy_1");
            
            var buff = new BuffInstance(
                "test_debuff",
                owner.Id,
                BuffKind.Debuff,
                new List<BuffEffect>()
            );

            owner.ApplyBuff(buff);

            Assert.Single(owner.Buffs);
            Assert.True(owner.Buffs.ContainsKey("test_debuff"));
        }

        [Fact]
        public void EnemyBuffOwner_ApplyBuff_StackPolicy_Works()
        {
            var enemy = new Enemy();
            var owner = new EnemyBuffOwner(enemy, "enemy_1");
            
            var buff1 = new BuffInstance(
                "stack_debuff",
                owner.Id,
                BuffKind.Debuff,
                new List<BuffEffect> { BuffEffect.DamageOverTime(5) },
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 5
            );

            owner.ApplyBuff(buff1);
            owner.ApplyBuff(new BuffInstance(
                "stack_debuff",
                owner.Id,
                BuffKind.Debuff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 5
            ));

            Assert.Equal(2, buff1.Stacks);
            Assert.Equal(10, buff1.GetDamagePerTick()); // 5 * 2 stacks
        }

        [Fact]
        public void EnemyBuffOwner_RemoveBuff_Works()
        {
            var enemy = new Enemy();
            var owner = new EnemyBuffOwner(enemy, "enemy_1");
            
            var buff = new BuffInstance(
                "test_debuff",
                owner.Id,
                BuffKind.Debuff,
                new List<BuffEffect>()
            );

            owner.ApplyBuff(buff);
            Assert.Single(owner.Buffs);

            bool removed = owner.RemoveBuff("test_debuff", "expired");
            Assert.True(removed);
            Assert.Empty(owner.Buffs);
        }

        [Fact]
        public void EnemyBuffOwner_ReceiveDamage_ReducesHp()
        {
            var enemy = new Enemy { Hp = 200, MaxHp = 300 };
            var owner = new EnemyBuffOwner(enemy, "enemy_1");

            owner.ReceiveDamage(50, new DamageMeta("player_attack"));

            Assert.Equal(150, enemy.Hp);
            Assert.Equal(150, owner.CurrentHp);
        }

        [Fact]
        public void EnemyBuffOwner_ReceiveDamage_ClampsToZero()
        {
            var enemy = new Enemy { Hp = 30, MaxHp = 300 };
            var owner = new EnemyBuffOwner(enemy, "enemy_1");

            owner.ReceiveDamage(100, new DamageMeta("skill"));

            Assert.Equal(0, enemy.Hp);
        }

        [Fact]
        public void EnemyBuffOwner_ReceiveHeal_IncreasesHp()
        {
            var enemy = new Enemy { Hp = 100, MaxHp = 300 };
            var owner = new EnemyBuffOwner(enemy, "enemy_1");

            owner.ReceiveHeal(50, new HealMeta("regen"));

            Assert.Equal(150, enemy.Hp);
        }

        [Fact]
        public void EnemyBuffOwner_ReceiveHeal_ClampsToMax()
        {
            var enemy = new Enemy { Hp = 280, MaxHp = 300 };
            var owner = new EnemyBuffOwner(enemy, "enemy_1");

            owner.ReceiveHeal(50, new HealMeta("regen"));

            Assert.Equal(300, enemy.Hp);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void BuffOwner_MultipleBuffs_CanCoexist()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");

            var buff1 = new BuffInstance(
                "buff1",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            );

            var buff2 = new BuffInstance(
                "buff2",
                owner.Id,
                BuffKind.Buff,
                new List<BuffEffect> { BuffEffect.StatAdditive("CritChancePercent", 0.05) }
            );

            var debuff = new BuffInstance(
                "debuff1",
                owner.Id,
                BuffKind.Debuff,
                new List<BuffEffect> { BuffEffect.StatReduction("HastePercent", 0.1) }
            );

            owner.ApplyBuff(buff1);
            owner.ApplyBuff(buff2);
            owner.ApplyBuff(debuff);

            Assert.Equal(3, owner.Buffs.Count);
            Assert.True(owner.Buffs.ContainsKey("buff1"));
            Assert.True(owner.Buffs.ContainsKey("buff2"));
            Assert.True(owner.Buffs.ContainsKey("debuff1"));
        }

        [Fact]
        public void BuffOwner_DoTBuff_CanBeAppliedAndQueried()
        {
            var enemy = new Enemy { Hp = 200, MaxHp = 300 };
            var owner = new EnemyBuffOwner(enemy, "enemy_1");

            var dotBuff = new BuffInstance(
                "burning",
                owner.Id,
                BuffKind.Debuff,
                new List<BuffEffect> { BuffEffect.DamageOverTime(10) },
                durationSec: 6.0,
                tickIntervalSec: 1.0
            );

            owner.ApplyBuff(dotBuff);

            Assert.True(dotBuff.HasDamageOverTime());
            Assert.Equal(10, dotBuff.GetDamagePerTick());
            
            // Simulate tick
            int tickCount = dotBuff.Tick(1.5);
            Assert.Equal(1, tickCount);
        }

        [Fact]
        public void CharacterBuffOwner_ApplyBuff_ValidatesOwnerId()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff = new BuffInstance(
                "test_buff",
                "wrong_owner_id",  // Wrong owner ID
                BuffKind.Buff,
                new List<BuffEffect>()
            );

            var ex = Assert.Throws<ArgumentException>(() => owner.ApplyBuff(buff));
            Assert.Contains("OwnerId", ex.Message);
        }

        [Fact]
        public void CharacterBuffOwner_ClearAllBuffs_RemovesAllBuffs()
        {
            var character = new Character();
            var owner = new CharacterBuffOwner(character, "test_char_id");
            
            var buff1 = new BuffInstance("buff1", owner.Id, BuffKind.Buff, new List<BuffEffect>());
            var buff2 = new BuffInstance("buff2", owner.Id, BuffKind.Buff, new List<BuffEffect>());
            
            owner.ApplyBuff(buff1);
            owner.ApplyBuff(buff2);
            Assert.Equal(2, owner.Buffs.Count);
            
            owner.ClearAllBuffs();
            Assert.Empty(owner.Buffs);
        }

        [Fact]
        public void CharacterBuffOwner_Id_UsesStableMemberId()
        {
            var character = new Character { ActiveCombatProfessionId = "warrior" };
            var owner = new CharacterBuffOwner(character, "stable_member_id");
            
            Assert.Equal("stable_member_id", owner.Id);
            
            // Change profession - ID should remain stable
            character.ActiveCombatProfessionId = "mage";
            Assert.Equal("stable_member_id", owner.Id);
        }

        [Fact]
        public void EnemyBuffOwner_ApplyBuff_ValidatesOwnerId()
        {
            var enemy = new Enemy();
            var owner = new EnemyBuffOwner(enemy, "enemy_1");
            
            var buff = new BuffInstance(
                "test_debuff",
                "wrong_owner_id",  // Wrong owner ID
                BuffKind.Debuff,
                new List<BuffEffect>()
            );

            var ex = Assert.Throws<ArgumentException>(() => owner.ApplyBuff(buff));
            Assert.Contains("OwnerId", ex.Message);
        }

        [Fact]
        public void EnemyBuffOwner_ClearAllBuffs_RemovesAllBuffs()
        {
            var enemy = new Enemy();
            var owner = new EnemyBuffOwner(enemy, "enemy_1");
            
            var buff1 = new BuffInstance("debuff1", owner.Id, BuffKind.Debuff, new List<BuffEffect>());
            var buff2 = new BuffInstance("debuff2", owner.Id, BuffKind.Debuff, new List<BuffEffect>());
            
            owner.ApplyBuff(buff1);
            owner.ApplyBuff(buff2);
            Assert.Equal(2, owner.Buffs.Count);
            
            owner.ClearAllBuffs();
            Assert.Empty(owner.Buffs);
        }

        #endregion
    }
}
