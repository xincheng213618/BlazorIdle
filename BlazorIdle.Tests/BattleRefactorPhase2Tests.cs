using Xunit;
using BlazorIdle.Game.Battle.Execution;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game;
using BlazorIdle.Game.Combat;
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

        #region DamageApplier Tests

        private SimClock CreateTestClock(int nowMs = 0)
        {
            var clock = new SimClock();
            if (nowMs > 0) clock.AdvanceBy(nowMs);
            return clock;
        }

        [Fact]
        public void DamageApplier_ProcessDamageInstances_ImmediateDamageCallsCallback()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000);
            var applier = new DamageApplier(queue, clock);
            
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0 } // Immediate
                }
            };

            var callbackCalled = false;
            DamageInstance? callbackInstance = null;

            // Act
            applier.ProcessDamageInstances(result, instance =>
            {
                callbackCalled = true;
                callbackInstance = instance;
            });

            // Assert
            Assert.True(callbackCalled);
            Assert.NotNull(callbackInstance);
            Assert.Equal(100, callbackInstance!.Damage);
            Assert.True(queue.IsEmpty); // 立即伤害不进队列
        }

        [Fact]
        public void DamageApplier_ProcessDamageInstances_DelayedDamageEnqueues()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000); // 1 second
            var applier = new DamageApplier(queue, clock);
            
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0.5 } // Delayed
                }
            };

            var callbackCalled = false;

            // Act
            applier.ProcessDamageInstances(result, _ => callbackCalled = true);

            // Assert
            Assert.False(callbackCalled); // 延迟伤害不立即调用回调
            Assert.False(queue.IsEmpty);
            Assert.Equal(1, queue.Count);
            Assert.Equal(1.5, queue.PeekNextTime()); // 1.0 + 0.5 = 1.5 秒
        }

        [Fact]
        public void DamageApplier_ProcessPendingDamages_DequeuesReadyDamages()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000);
            var applier = new DamageApplier(queue, clock);
            
            // 添加延迟伤害
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0.5 }
                }
            };
            applier.ProcessDamageInstances(result);

            // 推进时间到 1.5 秒
            clock.AdvanceBy(500);
            
            var appliedDamages = new List<DamageInstance>();

            // Act
            int count = applier.ProcessPendingDamages(instance => appliedDamages.Add(instance));

            // Assert
            Assert.Equal(1, count);
            Assert.Single(appliedDamages);
            Assert.Equal(100, appliedDamages[0].Damage);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void DamageApplier_OnTargetDeath_RemovesPendingDamages()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000);
            var applier = new DamageApplier(queue, clock);
            
            queue.Enqueue(new DamageInstance { Damage = 100, TargetId = "enemy_1", ApplyAtSec = 0.5 });
            queue.Enqueue(new DamageInstance { Damage = 100, TargetId = "enemy_2", ApplyAtSec = 0.5 });

            // Act
            int removed = applier.OnTargetDeath("enemy_1");

            // Assert
            Assert.Equal(1, removed);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void DamageApplier_OnBattleEnd_ClearsAllPendingDamages()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000);
            var applier = new DamageApplier(queue, clock);
            
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 0.5 });
            queue.Enqueue(new DamageInstance { Damage = 200, ApplyAtSec = 1.0 });

            // Act
            applier.OnBattleEnd();

            // Assert
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void DamageApplier_HasPendingDamages_ReturnsCorrectValue()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var applier = new DamageApplier(queue, clock);

            // Assert - 初始无待应用伤害
            Assert.False(applier.HasPendingDamages);
            Assert.Equal(0, applier.PendingCount);

            // Act - 添加延迟伤害
            queue.Enqueue(new DamageInstance { Damage = 100, ApplyAtSec = 1.0 });

            // Assert - 有待应用伤害
            Assert.True(applier.HasPendingDamages);
            Assert.Equal(1, applier.PendingCount);
        }

        [Fact]
        public void DamageApplier_DamageAppliedEvent_FiresCorrectly()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000);
            var applier = new DamageApplier(queue, clock);

            var events = new List<DamageAppliedEventArgs>();
            applier.DamageApplied += args => events.Add(args);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0 }
                }
            };

            // Act
            applier.ProcessDamageInstances(result);

            // Assert
            Assert.Single(events);
            Assert.True(events[0].IsImmediate);
            Assert.Equal(100, events[0].Instance.Damage);
            Assert.Equal(1000, events[0].AppliedAtMs);
        }

        #endregion

        #region SkillExecutor Tests

        private (SkillExecutor executor, SkillResolver resolver, DamageApplier applier, SimClock clock, SkillRepository repo) CreateTestExecutor()
        {
            var repo = new SkillRepository();
            var resolver = new SkillResolver(null, repo, null);
            var queue = new PendingDamageQueue();
            var clock = new SimClock();
            var applier = new DamageApplier(queue, clock);
            var executor = new SkillExecutor(resolver, applier, clock);

            return (executor, resolver, applier, clock, repo);
        }

        [Fact]
        public void SkillExecutor_Execute_ReturnsResult()
        {
            // Arrange
            var (executor, resolver, applier, clock, repo) = CreateTestExecutor();
            
            var skill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                CanCrit = false,
                Damage = new DamageDef { CoefAtk = 1.0 }
            };
            repo.RegisterSkill(skill);

            var ctx = CreateTestContext(clock);

            var request = new SkillExecutionRequest
            {
                CasterId = "player_1",
                SkillId = "test_skill",
                Context = ctx,
                SourceTrack = "attack",
                IsCasterPlayer = true
            };

            // Act
            var result = executor.Execute(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("test_skill", result.SkillId);
            Assert.Equal("player_1", result.CasterId);
            Assert.NotNull(result.CastResult);
        }

        [Fact]
        public void SkillExecutor_Execute_ProcessesMultiHitDamage()
        {
            // Arrange
            var (executor, resolver, applier, clock, repo) = CreateTestExecutor();
            
            var skill = new SkillDef
            {
                Id = "multi_hit_skill",
                Name = "Multi Hit Skill",
                CanCrit = false,
                Hits = new List<DamageHit>
                {
                    new DamageHit { HitIndex = 0, CoefAtk = 0.5, DelaySec = 0 },
                    new DamageHit { HitIndex = 1, CoefAtk = 0.5, DelaySec = 0.2 }
                }
            };
            repo.RegisterSkill(skill);

            var ctx = CreateTestContext(clock);

            var immediateDamages = new List<DamageInstance>();
            var request = new SkillExecutionRequest
            {
                CasterId = "player_1",
                SkillId = "multi_hit_skill",
                Context = ctx,
                SourceTrack = "attack",
                IsCasterPlayer = true,
                OnImmediateDamage = d => immediateDamages.Add(d)
            };

            // Act
            var result = executor.Execute(request);

            // Assert
            Assert.True(result.Success);
            Assert.Single(immediateDamages); // 只有第一段是立即伤害
            Assert.True(applier.HasPendingDamages); // 第二段是延迟伤害
        }

        [Fact]
        public void SkillExecutor_Execute_RaisesBuffOperationRequestedEvent()
        {
            // Arrange
            var (executor, resolver, applier, clock, repo) = CreateTestExecutor();
            
            var skill = new SkillDef
            {
                Id = "buff_skill",
                Name = "Buff Skill",
                OnCastBuffs = new List<BuffOperation>
                {
                    BuffOperation.ApplyByConfigId("test_buff")
                }
            };
            repo.RegisterSkill(skill);

            var ctx = CreateTestContext(clock);

            BuffOperationRequest? capturedRequest = null;
            executor.BuffOperationRequested += req => capturedRequest = req;

            var request = new SkillExecutionRequest
            {
                CasterId = "player_1",
                SkillId = "buff_skill",
                Context = ctx,
                SourceTrack = "attack",
                IsCasterPlayer = true
            };

            // Act
            executor.Execute(request);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("player_1", capturedRequest!.CasterId);
            Assert.Single(capturedRequest.Operations);
        }

        [Fact]
        public void SkillExecutor_Execute_RaisesAttackTriggerRequestedEvent()
        {
            // Arrange
            var (executor, resolver, applier, clock, repo) = CreateTestExecutor();
            
            var skill = new SkillDef
            {
                Id = "attack_skill",
                Name = "Attack Skill",
                CanCrit = false,
                Damage = new DamageDef { CoefAtk = 1.0 }
            };
            repo.RegisterSkill(skill);

            var ctx = CreateTestContext(clock);

            AttackTriggerRequest? capturedRequest = null;
            executor.AttackTriggerRequested += req => capturedRequest = req;

            var request = new SkillExecutionRequest
            {
                CasterId = "player_1",
                SkillId = "attack_skill",
                Context = ctx,
                SourceTrack = "attack",
                IsCasterPlayer = true,
                ProcessTriggers = true
            };

            // Act
            executor.Execute(request);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("player_1", capturedRequest!.CasterId);
            Assert.Equal("attack_skill", capturedRequest.SkillId);
        }

        [Fact]
        public void SkillExecutor_ProcessPendingDamages_ProcessesDelayedDamages()
        {
            // Arrange
            var (executor, resolver, applier, clock, repo) = CreateTestExecutor();
            
            var skill = new SkillDef
            {
                Id = "delayed_skill",
                Name = "Delayed Skill",
                CanCrit = false,
                Hits = new List<DamageHit>
                {
                    new DamageHit { HitIndex = 0, CoefAtk = 1.0, DelaySec = 0.5 }
                }
            };
            repo.RegisterSkill(skill);

            var ctx = CreateTestContext(clock);

            var request = new SkillExecutionRequest
            {
                CasterId = "player_1",
                SkillId = "delayed_skill",
                Context = ctx,
                SourceTrack = "attack",
                IsCasterPlayer = true
            };

            executor.Execute(request);
            Assert.True(applier.HasPendingDamages);

            // 推进时间
            clock.AdvanceBy(600);

            var processedDamages = new List<DamageInstance>();

            // Act
            int count = executor.ProcessPendingDamages(d => processedDamages.Add(d));

            // Assert
            Assert.Equal(1, count);
            Assert.Single(processedDamages);
            Assert.False(applier.HasPendingDamages);
        }

        private BattleContext CreateTestContext(SimClock clock)
        {
            var combatStats = new CombatStats
            {
                AttackFinal = 100,
                CritChancePercent = 0,
                CritDamageBonusPercent = 50
            };

            return new BattleContext
            {
                DamageCalculator = DamageCalculator.CreateDefault(),
                AttackerCombatStats = combatStats,
                AttackerHPRatio = 1.0,
                AttackerElement = ElementIds.Neutral,
                DefenderElement = ElementIds.Neutral,
                DefenderDamageReductionPercent = 0,
                Clock = clock,
                Rng = new RngContext(12345)
            };
        }

        #endregion
    }
}
