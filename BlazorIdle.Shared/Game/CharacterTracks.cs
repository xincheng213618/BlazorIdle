using System;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 角色战斗轨道 - 管理单个角色的攻击和技能轨道
    /// Character battle tracks - manages attack and skill tracks for a single character
    /// </summary>
    public class CharacterTracks
    {
        /// <summary>
        /// 角色ID
        /// Character ID
        /// </summary>
        public string CharacterId { get; }

        /// <summary>
        /// 角色引用
        /// Character reference
        /// </summary>
        public Character Character { get; }

        /// <summary>
        /// 普通攻击轨道
        /// Normal attack track
        /// </summary>
        public TrackState AttackTrack { get; }

        /// <summary>
        /// 特殊技能轨道
        /// Special skill track
        /// </summary>
        public TrackState SpecialTrack { get; }

        /// <summary>
        /// 是否已启用
        /// Whether tracks are enabled
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CharacterTracks(string characterId, Character character)
        {
            CharacterId = characterId;
            Character = character;

            // 创建攻击轨道
            var attackIntervalMs = 1000.0 / Math.Max(0.1, character.AttackRateAPS);
            AttackTrack = new TrackState(TrackType.Attack, attackIntervalMs);
            AttackTrack.SetHaste(1.0 + character.HastePercent / 100.0);

            // 创建特殊技能轨道
            var specialIntervalMs = Math.Max(100.0, character.SpecialIntervalSec * 1000.0);
            SpecialTrack = new TrackState(TrackType.Special, specialIntervalMs);

            IsEnabled = true;
        }

        /// <summary>
        /// 重置所有轨道
        /// Reset all tracks
        /// </summary>
        public void Reset(int nowMs)
        {
            AttackTrack.Reset(nowMs);
            SpecialTrack.Reset(nowMs);
            IsEnabled = true;
        }

        /// <summary>
        /// 暂停所有轨道
        /// Pause all tracks
        /// </summary>
        public void Pause()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// 恢复所有轨道
        /// Resume all tracks
        /// </summary>
        public void Resume()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// 暂停攻击轨道 - Phase 8: 施法时使用
        /// Pause attack track - Phase 8: Used during casting
        /// </summary>
        /// <param name="nowMs">当前时间（毫秒）/ Current time in milliseconds</param>
        public void PauseAttackTrack(int nowMs)
        {
            AttackTrack.Pause(nowMs);
        }

        /// <summary>
        /// 恢复攻击轨道 - Phase 8: 施法完成/中断时使用
        /// Resume attack track - Phase 8: Used when cast completes/interrupts
        /// </summary>
        /// <param name="nowMs">当前时间（毫秒）/ Current time in milliseconds</param>
        public void ResumeAttackTrack(int nowMs)
        {
            AttackTrack.Resume(nowMs);
        }

        /// <summary>
        /// 检查攻击轨道是否被暂停 - Phase 8
        /// Check if attack track is paused - Phase 8
        /// </summary>
        public bool IsAttackTrackPaused()
        {
            return AttackTrack.IsPaused;
        }

        /// <summary>
        /// 更新急速值
        /// Update haste value
        /// </summary>
        public void UpdateHaste(double hastePercent)
        {
            AttackTrack.SetHaste(1.0 + hastePercent / 100.0);
        }
    }

    /// <summary>
    /// 敌人战斗轨道 - 管理单个敌人的攻击轨道
    /// Enemy battle track - manages attack track for a single enemy
    /// </summary>
    public class EnemyTrack
    {
        /// <summary>
        /// 敌人ID
        /// Enemy ID
        /// </summary>
        public string EnemyId { get; }

        /// <summary>
        /// 敌人引用
        /// Enemy reference
        /// </summary>
        public Enemy Enemy { get; }

        /// <summary>
        /// 攻击轨道
        /// Attack track
        /// </summary>
        public TrackState AttackTrack { get; }

        /// <summary>
        /// 是否已启用
        /// Whether track is enabled
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public EnemyTrack(string enemyId, Enemy enemy)
        {
            EnemyId = enemyId;
            Enemy = enemy;

            var attackIntervalMs = Math.Max(100.0, enemy.AttackIntervalSec * 1000.0);
            AttackTrack = new TrackState(TrackType.EnemyAttack, attackIntervalMs);

            IsEnabled = true;
        }

        /// <summary>
        /// 重置轨道
        /// Reset track
        /// </summary>
        public void Reset(int nowMs)
        {
            AttackTrack.Reset(nowMs);
            IsEnabled = true;
        }

        /// <summary>
        /// 暂停轨道
        /// Pause track
        /// </summary>
        public void Pause()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// 恢复轨道
        /// Resume track
        /// </summary>
        public void Resume()
        {
            IsEnabled = true;
        }
    }
}