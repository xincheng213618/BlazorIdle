using BlazorIdle.Configuration;
using BlazorIdle.Shared.Models;
using System.Timers;
using Timer = System.Timers.Timer;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 心跳服务接口 - 定义角色数据自动保存功能
    /// Heartbeat service interface - defines auto-save functionality for character data
    /// </summary>
    public interface IHeartbeatService : IDisposable
    {
        /// <summary>
        /// 启动心跳服务，开始为指定角色定时保存数据
        /// Start heartbeat service, begin periodic save for specified character
        /// </summary>
        /// <param name="character">要保存的角色数据</param>
        void Start(CharacterData character);

        /// <summary>
        /// 停止心跳服务
        /// Stop heartbeat service
        /// </summary>
        void Stop();

        /// <summary>
        /// 更新当前跟踪的角色数据（不会立即保存，等待下一次心跳）
        /// Update currently tracked character data (won't save immediately, waits for next heartbeat)
        /// </summary>
        /// <param name="character">更新后的角色数据</param>
        void UpdateCharacter(CharacterData character);

        /// <summary>
        /// 手动触发一次保存
        /// Manually trigger a save
        /// </summary>
        Task SaveNowAsync();

        /// <summary>
        /// 心跳服务是否正在运行
        /// Whether heartbeat service is running
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    /// 心跳服务实现 - 负责角色数据的定时自动保存
    /// Heartbeat service implementation - handles periodic auto-save of character data
    /// 
    /// 设计思路：
    /// Design rationale:
    /// 1. 使用System.Timers.Timer实现定时触发
    ///    Uses System.Timers.Timer for periodic triggers
    /// 2. 监听角色切换事件，自动启停心跳
    ///    Listens to character switch events, auto start/stop heartbeat
    /// 3. 保存前检查数据是否有变化，避免不必要的网络请求
    ///    Checks for data changes before saving to avoid unnecessary network requests
    /// 4. 异常处理确保服务稳定运行
    ///    Exception handling ensures stable service operation
    /// </summary>
    public class HeartbeatService : IHeartbeatService
    {
        private readonly ICharacterService _characterService;
        private readonly HeartbeatConfiguration _config;
        private readonly ILogger<HeartbeatService> _logger;
        
        private Timer? _timer;
        private CharacterData? _currentCharacter;
        private bool _isRunning;
        private readonly object _lock = new object();

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public HeartbeatService(
            ICharacterService characterService,
            HeartbeatConfiguration config,
            ILogger<HeartbeatService> logger)
        {
            _characterService = characterService;
            _config = config;
            _logger = logger;

            // 订阅角色切换事件，实现自动启停
            // Subscribe to character switch event for auto start/stop
            _characterService.SelectedCharacterChanged += OnSelectedCharacterChanged;
        }

        /// <summary>
        /// 心跳服务是否正在运行
        /// Whether heartbeat service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isRunning;
                }
            }
        }

        /// <summary>
        /// 启动心跳服务
        /// Start heartbeat service
        /// </summary>
        public void Start(CharacterData character)
        {
            if (character == null)
            {
                _logger.LogWarning("Cannot start heartbeat: character is null");
                return;
            }

            // 检查配置是否启用自动保存
            // Check if auto-save is enabled in configuration
            if (!_config.EnableAutoSave)
            {
                _logger.LogInformation("Heartbeat auto-save is disabled in configuration");
                return;
            }

            lock (_lock)
            {
                // 如果已经在运行，先停止
                // If already running, stop first
                if (_isRunning)
                {
                    StopInternal();
                }

                _currentCharacter = character;
                
                // 创建并配置定时器
                // Create and configure timer
                _timer = new Timer(_config.SaveInterval.TotalMilliseconds);
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();
                
                _isRunning = true;
                
                _logger.LogInformation(
                    $"Heartbeat service started for character '{character.Name}' (ID: {character.Id}), " +
                    $"save interval: {_config.SaveIntervalSeconds} seconds");
            }
        }

        /// <summary>
        /// 停止心跳服务
        /// Stop heartbeat service
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();
            }
        }

        /// <summary>
        /// 内部停止方法（不加锁，由调用者负责加锁）
        /// Internal stop method (no locking, caller is responsible for locking)
        /// </summary>
        private void StopInternal()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Dispose();
                _timer = null;
            }

            if (_isRunning)
            {
                _logger.LogInformation(
                    $"Heartbeat service stopped for character '{_currentCharacter?.Name}' " +
                    $"(ID: {_currentCharacter?.Id})");
            }

            _isRunning = false;
            _currentCharacter = null;
        }

        /// <summary>
        /// 更新当前跟踪的角色数据
        /// Update currently tracked character data
        /// </summary>
        public void UpdateCharacter(CharacterData character)
        {
            lock (_lock)
            {
                if (_isRunning && _currentCharacter != null)
                {
                    // 只更新引用，不立即保存
                    // Only update reference, don't save immediately
                    _currentCharacter = character;
                }
            }
        }

        /// <summary>
        /// 手动触发保存
        /// Manually trigger save
        /// </summary>
        public async Task SaveNowAsync()
        {
            CharacterData? characterToSave;
            
            lock (_lock)
            {
                characterToSave = _currentCharacter;
            }

            if (characterToSave != null)
            {
                await SaveCharacterDataAsync(characterToSave);
            }
        }

        /// <summary>
        /// 定时器触发事件处理
        /// Timer elapsed event handler
        /// </summary>
        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            CharacterData? characterToSave;
            
            // 获取要保存的角色数据（最小化锁定时间）
            // Get character data to save (minimize lock time)
            lock (_lock)
            {
                characterToSave = _currentCharacter;
            }

            if (characterToSave != null)
            {
                await SaveCharacterDataAsync(characterToSave);
            }
        }

        /// <summary>
        /// 执行角色数据保存操作
        /// Execute character data save operation
        /// </summary>
        private async Task SaveCharacterDataAsync(CharacterData character)
        {
            try
            {
                _logger.LogDebug($"Heartbeat: Saving character '{character.Name}' (ID: {character.Id})");

                // 调用CharacterService更新角色数据
                // Call CharacterService to update character data
                var result = await _characterService.UpdateCharacterAsync(character.Id, character);

                if (result?.Success == true)
                {
                    _logger.LogDebug($"Heartbeat: Successfully saved character '{character.Name}'");
                }
                else
                {
                    _logger.LogWarning(
                        $"Heartbeat: Failed to save character '{character.Name}': {result?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                // 捕获异常但不中断心跳服务
                // Catch exception but don't interrupt heartbeat service
                _logger.LogError(ex, $"Heartbeat: Error saving character '{character.Name}' (ID: {character.Id})");
            }
        }

        /// <summary>
        /// 角色切换事件处理 - 自动启停心跳
        /// Character switch event handler - auto start/stop heartbeat
        /// </summary>
        private void OnSelectedCharacterChanged(CharacterData? character)
        {
            if (character == null)
            {
                // 角色被取消选择，停止心跳
                // Character deselected, stop heartbeat
                Stop();
            }
            else
            {
                // 新角色被选中，启动心跳
                // New character selected, start heartbeat
                Start(character);
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // 取消订阅事件
            // Unsubscribe from events
            _characterService.SelectedCharacterChanged -= OnSelectedCharacterChanged;
            
            // 停止并清理定时器
            // Stop and cleanup timer
            Stop();
        }
    }
}
