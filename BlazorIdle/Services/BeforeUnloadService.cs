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
        private DotNetObjectReference<BeforeUnloadService>? _objRef;
        private bool _isInitialized = false;

        public BeforeUnloadService(
            IJSRuntime jsRuntime,
            ILogger<BeforeUnloadService> logger,
            ICharacterService characterService)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
            _characterService = characterService;
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
                _logger.LogInformation("BeforeUnload triggered - attempting to save character");

                // 获取当前选中的角色
                var character = await _characterService.GetSelectedCharacterAsync();
                
                if (character != null)
                {
                    // 尝试立即保存
                    var saved = await _characterService.SaveImmediatelyAsync(character, "页面关闭保存");
                    
                    if (saved)
                    {
                        _logger.LogInformation("Successfully saved character {CharacterId} before unload", character.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to save character {CharacterId} before unload", character.Id);
                    }
                }
                else
                {
                    _logger.LogDebug("No character selected, skipping save on unload");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving character before unload");
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
