using System.Collections.Generic;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Tracks
{
    /// <summary>
    /// Attack Track Legacy 适配器 - 封装现有的玩家攻击触发逻辑
    /// Attack Track Legacy adapter - encapsulates existing player attack trigger logic
    /// </summary>
    public sealed class AttackTrackLegacy : ITrack
    {
        private readonly TrackState _trackState;
        private readonly ISkillResolver _skillResolver;
        private readonly TrackConfig _config;
        private bool _isSuspended = false;

        /// <summary>
        /// Track ID
        /// </summary>
        public string Id => "attack";

        /// <summary>
        /// 是否已暂停
        /// Whether suspended
        /// </summary>
        public bool IsSuspended => _isSuspended;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="trackState">内部 TrackState 实例（来自 CharacterTracks.AttackTrack）</param>
        /// <param name="skillResolver">技能解析器</param>
        /// <param name="config">Track 配置</param>
        public AttackTrackLegacy(TrackState trackState, ISkillResolver skillResolver, TrackConfig config)
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
            // 预留：记录暂停原因用于调试
            // Reserved: log suspension reason for debugging
        }

        /// <summary>
        /// 恢复 Track（预留）
        /// Resume track (reserved)
        /// </summary>
        public void Resume(string reason)
        {
            _isSuspended = false;
            // 预留：记录恢复原因用于调试
            // Reserved: log resumption reason for debugging
        }

        /// <summary>
        /// 推进 Track 时间并触发技能
        /// Advance track time and trigger skills
        /// </summary>
        public void Tick(double dt, BattleContext ctx)
        {
            if (_isSuspended) return;

            // 收集触发次数（通过现有 TrackState 逻辑）
            // Collect trigger count (using existing TrackState logic)
            int nowMs = ctx.Clock.NowMs;
            int triggerCount = _trackState.CollectTriggers(nowMs);

            // 为每次触发调用 SkillResolver
            // Call SkillResolver for each trigger
            for (int i = 0; i < triggerCount; i++)
            {
                var opts = new SkillCastOptions
                {
                    SourceTrack = "attack",
                    ForceCrit = false
                };

                // 使用 CastBundle 施放绑定的技能
                // Use CastBundle to cast bound skills
                _skillResolver.CastBundle(_config.BoundSkills, ctx, opts);
            }
        }
    }
}
