using System;
using System.Collections.Generic;

namespace BlazorIdle.Game
{
    public sealed class SegmentAggregatorOptions
    {
        public int MaxEvents { get; set; } = 12;
        public int MaxDurationMs { get; set; } = 2000;
    }

    public sealed class SegmentAggregator
    {
        private readonly SegmentAggregatorOptions _opt;

        private bool _hasOpen;
        private int _startMs;
        private int _lastEventMs;
        private int _rngStart;
        private int _rngEnd;
        private int _eventCount;
        private readonly Dictionary<string, int> _damageBySource = new();

        public SegmentAggregator(SegmentAggregatorOptions? opt = null)
        {
            _opt = opt ?? new SegmentAggregatorOptions();
        }

        public void Reset()
        {
            _hasOpen = false;
            _damageBySource.Clear();
            _eventCount = 0;
            _startMs = 0;
            _lastEventMs = 0;
            _rngStart = 0;
            _rngEnd = 0;
        }

        // 添加一个事件；如果达到阈值，返回新 Segment
        public CombatSegment? AddEvent(CombatEvent e)
        {
            if (!_hasOpen)
            {
                _hasOpen = true;
                _startMs = e.TimeMs;
                _rngStart = e.RngIndexAfter; // 开段时的 RNG 可设置为第一事件后的索引，也可由外部传入开始前索引
            }

            _lastEventMs = e.TimeMs;
            _rngEnd = e.RngIndexAfter;
            _eventCount++;

            var key = e.Source.ToString();
            _damageBySource.TryGetValue(key, out var cur);
            _damageBySource[key] = cur + e.Damage;

            // 事件阈值
            if (_eventCount >= _opt.MaxEvents)
            {
                return FlushInternal();
            }

            return null;
        }

        // 按时间阈值检测（即使没有新事件，也会把正在积累的段在够时长后刷出）
        public CombatSegment? Tick(int nowMs, int rngIndexNow)
        {
            if (!_hasOpen) return null;

            // 没有事件则不给段；有事件且时长达到阈值才刷
            var duration = nowMs - _startMs;
            if (duration >= _opt.MaxDurationMs && _eventCount > 0)
            {
                _rngEnd = Math.Max(_rngEnd, rngIndexNow);
                return FlushInternal(nowOverride: nowMs);
            }

            return null;
        }

        public CombatSegment? ForceFlush(int nowMs, int rngIndexNow)
        {
            if (!_hasOpen || _eventCount <= 0) return null;
            _rngEnd = Math.Max(_rngEnd, rngIndexNow);
            return FlushInternal(nowOverride: Math.Max(_lastEventMs, nowMs));
        }

        private CombatSegment FlushInternal(int? nowOverride = null)
        {
            var seg = new CombatSegment
            {
                StartMs = _startMs,
                EndMs = nowOverride ?? _lastEventMs,
                EventCount = _eventCount,
                DamageBySource = new Dictionary<string, int>(_damageBySource),
                RngIndexStart = _rngStart,
                RngIndexEnd = _rngEnd
            };

            // reset
            _damageBySource.Clear();
            _eventCount = 0;
            _hasOpen = false;
            _startMs = 0;
            _lastEventMs = 0;
            _rngStart = 0;
            _rngEnd = 0;

            return seg;
        }
    }
}