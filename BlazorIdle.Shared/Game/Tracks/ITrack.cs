using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Tracks
{
    /// <summary>
    /// Track 抽象接口 - 负责"何时触发"，与"效果处理"解耦
    /// Track abstract interface - responsible for "when to trigger", decoupled from "effect processing"
    /// </summary>
    public interface ITrack
    {
        /// <summary>
        /// Track ID（如 "attack", "special", "enemy_attack"）
        /// Track ID (e.g., "attack", "special", "enemy_attack")
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 是否已暂停（预留：施法/控制暂停）
        /// Whether suspended (reserved: for casting/control suspension)
        /// </summary>
        bool IsSuspended { get; }

        /// <summary>
        /// 暂停 Track（预留）
        /// Suspend track (reserved)
        /// </summary>
        /// <param name="reason">暂停原因（用于调试）</param>
        void Suspend(string reason);

        /// <summary>
        /// 恢复 Track（预留）
        /// Resume track (reserved)
        /// </summary>
        /// <param name="reason">恢复原因（用于调试）</param>
        void Resume(string reason);

        /// <summary>
        /// 推进 Track 时间
        /// Advance track time
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        /// <param name="ctx">战斗上下文</param>
        void Tick(double dt, BattleContext ctx);
    }
}
