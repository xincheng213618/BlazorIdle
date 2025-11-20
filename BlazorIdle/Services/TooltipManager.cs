using System;

namespace BlazorIdle.Services
{
    /// <summary>
    /// Phase 10.3: 管理全局 Tooltip 显示状态，确保同一时间只显示一个 Tooltip
    /// Phase 10.3: Manages global tooltip display state to ensure only one tooltip is shown at a time
    /// </summary>
    public class TooltipManager
    {
        private static readonly TooltipManager _instance = new TooltipManager();
        
        /// <summary>
        /// 获取单例实例
        /// Get singleton instance
        /// </summary>
        public static TooltipManager Instance => _instance;
        
        /// <summary>
        /// 当前激活的 Tooltip ID
        /// Currently active tooltip ID
        /// </summary>
        private string? _activeTooltipId;
        
        /// <summary>
        /// Tooltip 隐藏回调
        /// Tooltip hide callback
        /// </summary>
        private Action? _hideCallback;
        
        /// <summary>
        /// 注册并显示 Tooltip
        /// Register and show tooltip
        /// </summary>
        /// <param name="tooltipId">Tooltip 唯一标识 / Unique tooltip identifier</param>
        /// <param name="hideCallback">隐藏回调（用于隐藏旧 Tooltip）/ Hide callback (to hide old tooltip)</param>
        public void ShowTooltip(string tooltipId, Action hideCallback)
        {
            // 如果有不同的 Tooltip 正在显示，先隐藏它
            // If a different tooltip is showing, hide it first
            if (_activeTooltipId != null && _activeTooltipId != tooltipId)
            {
                _hideCallback?.Invoke();
            }
            
            _activeTooltipId = tooltipId;
            _hideCallback = hideCallback;
        }
        
        /// <summary>
        /// 隐藏 Tooltip
        /// Hide tooltip
        /// </summary>
        /// <param name="tooltipId">Tooltip 唯一标识 / Unique tooltip identifier</param>
        public void HideTooltip(string tooltipId)
        {
            // 只有当前激活的 Tooltip 才能隐藏
            // Only the currently active tooltip can be hidden
            if (_activeTooltipId == tooltipId)
            {
                _activeTooltipId = null;
                _hideCallback = null;
            }
        }
    }
}
