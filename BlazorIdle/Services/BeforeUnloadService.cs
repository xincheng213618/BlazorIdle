using Microsoft.JSInterop;

namespace BlazorIdle.Services
{
    /// <summary>
    /// Task 4.5: 页面关闭前处理服务
    /// Service for handling actions before page unload
    /// </summary>
    public class BeforeUnloadService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<BeforeUnloadService> _logger;
        private readonly ICharacterService _characterService;
        private readonly IHeartbeatService _heartbeatService;
        private DotNetObjectReference<BeforeUnloadService>? _objRef;
        private bool _isInitialized = false;

        public BeforeUnloadService(
            IJSRuntime jsRuntime,
            ILogger<BeforeUnloadService> logger,
            ICharacterService characterService,
            IHeartbeatService heartbeatService)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
            _characterService = characterService;
            _heartbeatService = heartbeatService;
        }

        /// <summary>
        /// 初始化beforeunload事件监听
        /// Initialize beforeunload event listener
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogDebug("BeforeUnloadService already initialized");
                return;
            }

            try
            {
                _objRef = DotNetObjectReference.Create(this);

                // 注册JavaScript的beforeunload事件处理
                await _jsRuntime.InvokeVoidAsync("blazorIdle.registerBeforeUnload", _objRef);

                _isInitialized = true;
                _logger.LogInformation("BeforeUnloadService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize BeforeUnloadService");
            }
        }

        /// <summary>
        /// 当页面即将关闭时调用（由JavaScript触发）
        /// Called when page is about to unload (triggered by JavaScript)
        /// </summary>
        [JSInvokable]
        public async Task OnBeforeUnload()
        {
            try
            {
                _logger.LogWarning("=== BeforeUnload triggered - attempting to save character ===");
                Console.WriteLine("=== BeforeUnload triggered - attempting to save character ===");

                // 使用HeartbeatService立即保存当前跟踪的角色数据
                // Use HeartbeatService to immediately save currently tracked character data
                if (_heartbeatService.IsRunning)
                {
                    _logger.LogWarning("HeartbeatService is running, triggering save");
                    Console.WriteLine("HeartbeatService is running, triggering save");
                    
                    await _heartbeatService.SaveNowAsync();
                    
                    _logger.LogWarning("=== Save completed via HeartbeatService ===");
                    Console.WriteLine("=== Save completed via HeartbeatService ===");
                }
                else
                {
                    _logger.LogWarning("HeartbeatService NOT running - cannot save!");
                    Console.WriteLine("HeartbeatService NOT running - cannot save!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving character before unload");
                Console.WriteLine($"Error in OnBeforeUnload: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_isInitialized && _objRef != null)
                {
                    // 注销JavaScript的beforeunload事件处理
                    await _jsRuntime.InvokeVoidAsync("blazorIdle.unregisterBeforeUnload");
                }

                _objRef?.Dispose();
                _logger.LogInformation("BeforeUnloadService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing BeforeUnloadService");
            }
        }
    }
}
