using System.Collections.Generic;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Tracks
{
    /// <summary>
    /// Special Track Legacy 适配器 - 封装现有的玩家特殊技能触发逻辑
    /// Special Track Legacy adapter - encapsulates existing player special skill trigger logic
    /// </summary>
    public sealed class SpecialTrackLegacy : ITrack
    {
        private readonly TrackState _trackState;
        private readonly ISkillResolver _skillResolver;
        private readonly TrackConfig _config;
        private bool _isSuspended = false;

        /// <summary>
        /// Track ID
        /// </summary>
        public string Id => "special";

        /// <summary>
        /// 是否已暂停
        /// Whether suspended
        /// </summary>
        public bool IsSuspended => _isSuspended;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="trackState">内部 TrackState 实例（来自 CharacterTracks.SpecialTrack）</param>
        /// <param name="skillResolver">技能解析器</param>
        /// <param name="config">Track 配置</param>
        public SpecialTrackLegacy(TrackState trackState, ISkillResolver skillResolver, TrackConfig config)
        {
            _trackState = trackState;
            _skillResolver = skillResolver;
            _config = config;
        }

        /// <summary>
        /// 暂停 Track（预留）
        /// Suspend track (reserved)
        /// </summary>
        public void Suspend(string reason)
        {
            _isSuspended = true;
        }

        /// <summary>
        /// 恢复 Track（预留）
        /// Resume track (reserved)
        /// </summary>
        public void Resume(string reason)
        {
            _isSuspended = false;
        }

        /// <summary>
        /// 检查是否应该推进 Track（双模式门控）
        /// Check if track should advance (dual-mode gate)
        /// </summary>
        private bool ShouldAdvance(BattleContext ctx)
        {
            // 判断是否在遭遇中（简化：检查有战斗上下文即为 active）
            // Determine if in encounter (simplified: having battle context means active)
            bool encounterActive = ctx.PlayerTeam != null || ctx.Player != null;

            // 判断是否有敌人存活（用于 Presence 模式）
            // Determine if enemies are alive (for Presence mode)
            bool enemiesAlive = false;
            if (ctx.EnemyTeam != null)
            {
                enemiesAlive = ctx.EnemyTeam.AliveCount > 0;
            }
            else if (ctx.Enemy != null)
            {
                enemiesAlive = ctx.Enemy.Hp > 0;
            }

            // 根据进度策略决定是否推进
            // Decide whether to advance based on progress policy
            if (_config.ProgressPolicy == ProgressPolicy.Presence)
            {
                return encounterActive && enemiesAlive && !_isSuspended;
            }
            else // Encounter 模式
            {
                return encounterActive && !_isSuspended;
            }
        }

        /// <summary>
        /// 推进 Track 时间并触发技能
        /// Advance track time and trigger skills
        /// </summary>
        public void Tick(double dt, BattleContext ctx)
        {
            // 检查是否应该推进
            // Check if should advance
            if (!ShouldAdvance(ctx)) return;

            // 收集触发次数
            // Collect trigger count
            int nowMs = ctx.Clock.NowMs;
            int triggerCount = _trackState.CollectTriggers(nowMs);

            // 为每次触发调用 SkillResolver
            // Call SkillResolver for each trigger
            for (int i = 0; i < triggerCount; i++)
            {
                var opts = new SkillCastOptions
                {
                    SourceTrack = "special",
                    ForceCrit = false
                };

                // 使用 CastBundle 施放绑定的技能
                // Use CastBundle to cast bound skills
                _skillResolver.CastBundle(_config.BoundSkills, ctx, opts);
            }
        }
    }
}
