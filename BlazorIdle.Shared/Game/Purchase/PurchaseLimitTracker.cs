namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 限购追踪器 - 管理每日/账号/角色限购计数和自动重置
    /// Purchase limit tracker - manages per-day/account/character purchase counts and auto-reset
    /// </summary>
    public class PurchaseLimitTracker
    {
        private readonly PurchaseState _state;

        public PurchaseLimitTracker(PurchaseState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            EnsureDailyReset();
        }

        /// <summary>
        /// 检查剩余可购买次数
        /// Check remaining purchases
        /// </summary>
        public LimitCheckResult CheckRemaining(
            string itemId,
            string characterId,
            PurchaseLimit limits)
        {
            EnsureDailyReset();

            // 检查每日限购
            if (limits.PerDay > 0)
            {
                int dayCount = GetDayCount(characterId, itemId);
                if (dayCount >= limits.PerDay)
                {
                    return LimitCheckResult.Exhausted("已达每日购买上限");
                }
            }

            // 检查账号限购
            if (limits.PerAccount > 0)
            {
                int accountCount = GetAccountCount(itemId);
                if (accountCount >= limits.PerAccount)
                {
                    return LimitCheckResult.Exhausted("已达账号购买上限");
                }
            }

            // 检查角色限购
            if (limits.PerCharacter > 0)
            {
                int charCount = GetCharacterCount(characterId, itemId);
                if (charCount >= limits.PerCharacter)
                {
                    return LimitCheckResult.Exhausted("已达角色购买上限");
                }
            }

            // 计算剩余次数（取最小值）
            int remaining = -1; // -1 表示无限制
            if (limits.PerDay > 0)
            {
                int dayRemaining = limits.PerDay - GetDayCount(characterId, itemId);
                remaining = remaining == -1 ? dayRemaining : Math.Min(remaining, dayRemaining);
            }
            if (limits.PerAccount > 0)
            {
                int accountRemaining = limits.PerAccount - GetAccountCount(itemId);
                remaining = remaining == -1 ? accountRemaining : Math.Min(remaining, accountRemaining);
            }
            if (limits.PerCharacter > 0)
            {
                int charRemaining = limits.PerCharacter - GetCharacterCount(characterId, itemId);
                remaining = remaining == -1 ? charRemaining : Math.Min(remaining, charRemaining);
            }

            return LimitCheckResult.Available(remaining);
        }

        /// <summary>
        /// 增加购买计数
        /// Increment purchase count
        /// </summary>
        public void IncrementCount(string itemId, string characterId, PurchaseLimit limits)
        {
            if (limits.PerDay > 0)
                IncrementDayCount(characterId, itemId);
            if (limits.PerAccount > 0)
                IncrementAccountCount(itemId);
            if (limits.PerCharacter > 0)
                IncrementCharacterCount(characterId, itemId);
        }

        /// <summary>
        /// 确保每日重置（本地时间）
        /// Ensure daily reset (local time)
        /// </summary>
        private void EnsureDailyReset()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var lastReset = DateOnly.Parse(_state.LastDailyReset);

            if (lastReset < today)
            {
                // 重置每日计数
                _state.PerDayCountsByChar.Clear();
                _state.LastDailyReset = today.ToString("O");
            }
        }

        private int GetDayCount(string characterId, string itemId)
        {
            if (!_state.PerDayCountsByChar.TryGetValue(characterId, out var charCounts))
                return 0;
            return charCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        private int GetAccountCount(string itemId)
        {
            return _state.PerAccountCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        private int GetCharacterCount(string characterId, string itemId)
        {
            if (!_state.PerCharacterCounts.TryGetValue(characterId, out var charCounts))
                return 0;
            return charCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        private void IncrementDayCount(string characterId, string itemId)
        {
            if (!_state.PerDayCountsByChar.ContainsKey(characterId))
                _state.PerDayCountsByChar[characterId] = new Dictionary<string, int>();

            if (!_state.PerDayCountsByChar[characterId].ContainsKey(itemId))
                _state.PerDayCountsByChar[characterId][itemId] = 0;

            _state.PerDayCountsByChar[characterId][itemId]++;
        }

        private void IncrementAccountCount(string itemId)
        {
            if (!_state.PerAccountCounts.ContainsKey(itemId))
                _state.PerAccountCounts[itemId] = 0;

            _state.PerAccountCounts[itemId]++;
        }

        private void IncrementCharacterCount(string characterId, string itemId)
        {
            if (!_state.PerCharacterCounts.ContainsKey(characterId))
                _state.PerCharacterCounts[characterId] = new Dictionary<string, int>();

            if (!_state.PerCharacterCounts[characterId].ContainsKey(itemId))
                _state.PerCharacterCounts[characterId][itemId] = 0;

            _state.PerCharacterCounts[characterId][itemId]++;
        }
    }
}
