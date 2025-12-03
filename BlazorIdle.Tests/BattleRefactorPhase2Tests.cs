using Xunit;
using BlazorIdle.Game.Battle.Execution;
using BlazorIdle.Game.Skills;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 2 测试 - PendingDamageQueue
    /// Core Battle System Refactoring Phase 2 Tests - PendingDamageQueue
    /// </summary>
    public class BattleRefactorPhase2Tests
    {
        #region PendingDamageQueue Basic Tests

        [Fact]
        public void PendingDamageQueue_NewQueue_IsEmpty()
        {
            // Arrange & Act
            var queue = new PendingDamageQueue();

            // Assert
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, queue.Count);
            Assert.Null(queue.PeekNextTime());
        }

        [Fact]
        public void PendingDamageQueue_Enqueue_IncreasesCount()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var instance = new DamageInstance { Damage = 100, ApplyAtSec = 1.0 };

            // Act
            queue.Enqueue(instance);

            // Assert
            Assert.False(queue.IsEmpty);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void PendingDamageQueue_Enqueue_UpdatesNextTime()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var instance = new DamageInstance { Damage = 100, ApplyAtSec = 1.5 };

            // Act
            queue.Enqueue(instance, baseTimeSec: 0);

            // Assert
            Assert.Equal(1.5, queue.PeekNextTime());
        }

        [Fact]
        public void PendingDamageQueue_Enqueue_WithBaseTime_CalculatesAbsoluteTime()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var instance = new DamageInstance { Damage = 100, ApplyAtSec = 0.5 };

            // Act
            queue.Enqueue(instance, baseTimeSec: 2.0);

            // Assert
            Assert.Equal(2.5, queue.PeekNextTime());
        }

        [Fact]
        public void PendingDamageQueue_Clear_RemovesAll()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });
            queue.Enqueue(new DamageInstance { Damage = 200, ApplyAtSec = 2.0 });

            // Act
            queue.Clear();

            // Assert
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, queue.Count);
        }

        #endregion

        #region PendingDamageQueue Ordering Tests

        [Fact]
        public void PendingDamageQueue_Enqueue_MaintainsTimeOrder()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            
            // Act - 乱序入队
            queue.Enqueue(new DamageInstance { Damage = 300, ApplyAtSec = 3.0 });
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });
            queue.Enqueue(new DamageInstance { Damage = 200, ApplyAtSec = 2.0 });

            // Assert - 最早的应该在前面
            Assert.Equal(1.0, queue.PeekNextTime());
        }

        [Fact]
        public void PendingDamageQueue_DequeueReady_ReturnsInTimeOrder()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 300, ApplyAtSec = 3.0, HitIndex = 3 });
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0, HitIndex = 1 });
            queue.Enqueue(new DamageInstance { Damage = 200, ApplyAtSec = 2.0, HitIndex = 2 });

            // Act - 取出时间 <= 2.5 的所有伤害
            var ready = queue.DequeueReady(2.5);

            // Assert
            Assert.Equal(2, ready.Count);
            Assert.Equal(1, ready[0].HitIndex); // 1.0 秒的先出
            Assert.Equal(2, ready[1].HitIndex); // 2.0 秒的后出
            Assert.Equal(1, queue.Count); // 还剩 3.0 秒的
        }

        [Fact]
        public void PendingDamageQueue_DequeueReady_HandlesMultipleSameTime()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0, HitIndex = 1 });
            queue.Enqueue(new DamageInstance { Damage = 200, ApplyAtSec = 1.0, HitIndex = 2 });
            queue.Enqueue(new DamageInstance { Damage = 300, ApplyAtSec = 1.0, HitIndex = 3 });

            // Act
            var ready = queue.DequeueReady(1.0);

            // Assert
            Assert.Equal(3, ready.Count);
            Assert.True(queue.IsEmpty);
        }

        #endregion

        #region PendingDamageQueue DequeueReady Tests

        [Fact]
        public void PendingDamageQueue_DequeueReady_ReturnsEmptyWhenNoneReady()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 5.0 });

            // Act
            var ready = queue.DequeueReady(2.0);

            // Assert
            Assert.Empty(ready);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void PendingDamageQueue_DequeueReady_RemovesFromQueue()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });

            // Act
            var ready = queue.DequeueReady(2.0);

            // Assert
            Assert.Single(ready);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void PendingDamageQueue_DequeueReady_ExactTimeMatch()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });

            // Act - 恰好等于应用时间
            var ready = queue.DequeueReady(1.0);

            // Assert
            Assert.Single(ready);
            Assert.True(queue.IsEmpty);
        }

        #endregion

        #region PendingDamageQueue RemoveByTarget Tests

        [Fact]
        public void PendingDamageQueue_RemoveByTarget_RemovesMatchingDamages()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { TargetId = "enemy_1", ApplyAtSec = 1.0 });
            queue.Enqueue(new DamageInstance { TargetId = "enemy_2", ApplyAtSec = 2.0 });
            queue.Enqueue(new DamageInstance { TargetId = "enemy_1", ApplyAtSec = 3.0 });

            // Act
            int removed = queue.RemoveByTarget("enemy_1");

            // Assert
            Assert.Equal(2, removed);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void PendingDamageQueue_RemoveByTarget_NoMatchReturnsZero()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { TargetId = "enemy_1", ApplyAtSec = 1.0 });

            // Act
            int removed = queue.RemoveByTarget("enemy_999");

            // Assert
            Assert.Equal(0, removed);
            Assert.Equal(1, queue.Count);
        }

        #endregion

        #region PendingDamageQueue RemoveBySkill Tests

        [Fact]
        public void PendingDamageQueue_RemoveBySkill_RemovesMatchingDamages()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { SkillId = "skill_a", ApplyAtSec = 1.0 });
            queue.Enqueue(new DamageInstance { SkillId = "skill_b", ApplyAtSec = 2.0 });
            queue.Enqueue(new DamageInstance { SkillId = "skill_a", ApplyAtSec = 3.0 });

            // Act
            int removed = queue.RemoveBySkill("skill_a");

            // Assert
            Assert.Equal(2, removed);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void PendingDamageQueue_RemoveBySkill_WithBundleId_RemovesSpecific()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { SkillId = "skill_a", BundleId = "bundle_1", ApplyAtSec = 1.0 });
            queue.Enqueue(new DamageInstance { SkillId = "skill_a", BundleId = "bundle_2", ApplyAtSec = 2.0 });
            queue.Enqueue(new DamageInstance { SkillId = "skill_a", BundleId = "bundle_1", ApplyAtSec = 3.0 });

            // Act
            int removed = queue.RemoveBySkill("skill_a", "bundle_1");

            // Assert
            Assert.Equal(2, removed);
            Assert.Equal(1, queue.Count);
        }

        #endregion

        #region PendingDamageQueue GetAll Tests

        [Fact]
        public void PendingDamageQueue_GetAll_ReturnsAllInOrder()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 300, ApplyAtSec = 3.0 });
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });
            queue.Enqueue(new DamageInstance { Damage = 200, ApplyAtSec = 2.0 });

            // Act
            var all = queue.GetAll();

            // Assert
            Assert.Equal(3, all.Count);
            Assert.Equal(100, all[0].Damage); // 1.0 秒的
            Assert.Equal(200, all[1].Damage); // 2.0 秒的
            Assert.Equal(300, all[2].Damage); // 3.0 秒的
        }

        [Fact]
        public void PendingDamageQueue_GetAll_DoesNotModifyQueue()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });

            // Act
            var all = queue.GetAll();

            // Assert
            Assert.Equal(1, queue.Count);
            Assert.Single(all);
        }

        #endregion

        #region PendingDamageQueue Integration Tests

        [Fact]
        public void PendingDamageQueue_CompleteScenario_MultiHitSkill()
        {
            // Arrange - 模拟三连斩
            var queue = new PendingDamageQueue();
            var baseTime = 1.0;
            
            // 模拟技能施放时入队
            queue.Enqueue(new DamageInstance { Damage = 50, ApplyAtSec = 0, HitIndex = 0, SkillId = "triple_slash" }, baseTime);
            queue.Enqueue(new DamageInstance { Damage = 50, ApplyAtSec = 0.2, HitIndex = 1, SkillId = "triple_slash" }, baseTime);
            queue.Enqueue(new DamageInstance { Damage = 50, ApplyAtSec = 0.4, HitIndex = 2, SkillId = "triple_slash" }, baseTime);

            // Act & Assert - 分时间取出
            // T=1.0: 第一段应该可以取出
            var ready1 = queue.DequeueReady(1.0);
            Assert.Single(ready1);
            Assert.Equal(0, ready1[0].HitIndex);
            
            // T=1.15: 还没到第二段
            var ready2 = queue.DequeueReady(1.15);
            Assert.Empty(ready2);
            
            // T=1.25: 第二段可以取出
            var ready3 = queue.DequeueReady(1.25);
            Assert.Single(ready3);
            Assert.Equal(1, ready3[0].HitIndex);
            
            // T=1.5: 第三段可以取出
            var ready4 = queue.DequeueReady(1.5);
            Assert.Single(ready4);
            Assert.Equal(2, ready4[0].HitIndex);
            
            // 队列应该为空
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void PendingDamageQueue_CompleteScenario_DelayedExplosion()
        {
            // Arrange - 模拟延迟爆炸
            var queue = new PendingDamageQueue();
            var castTime = 2.0;
            
            queue.Enqueue(new DamageInstance 
            { 
                Damage = 200, 
                ApplyAtSec = 1.5, 
                SkillId = "delayed_explosion",
                TargetId = "enemy_1"
            }, castTime);

            // Assert - 应用时间应该是 2.0 + 1.5 = 3.5
            Assert.Equal(3.5, queue.PeekNextTime());

            // Act - 在 3.0 秒时还没到
            var ready1 = queue.DequeueReady(3.0);
            Assert.Empty(ready1);

            // Act - 在 3.5 秒时爆炸
            var ready2 = queue.DequeueReady(3.5);
            Assert.Single(ready2);
            Assert.Equal(200, ready2[0].Damage);
        }

        [Fact]
        public void PendingDamageQueue_CompleteScenario_TargetDiesBeforeDamage()
        {
            // Arrange - 延迟伤害
            var queue = new PendingDamageQueue();
            queue.Enqueue(new DamageInstance 
            { 
                Damage = 100, 
                ApplyAtSec = 2.0, 
                TargetId = "enemy_1" 
            });
            queue.Enqueue(new DamageInstance 
            { 
                Damage = 100, 
                ApplyAtSec = 3.0, 
                TargetId = "enemy_1" 
            });
            queue.Enqueue(new DamageInstance 
            { 
                Damage = 100, 
                ApplyAtSec = 2.5, 
                TargetId = "enemy_2" 
            });

            // Act - 目标死亡，移除其所有待应用伤害
            int removed = queue.RemoveByTarget("enemy_1");

            // Assert
            Assert.Equal(2, removed);
            Assert.Equal(1, queue.Count); // 只剩 enemy_2 的伤害
        }

        #endregion
    }
}
