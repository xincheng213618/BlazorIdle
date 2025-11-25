using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorIdle.Server.Data;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Shared.Models;
using BlazorIdle.Shared.Constants;
using BlazorIdle.Game.Config;
using System.Security.Claims;

namespace BlazorIdle.Server.Controllers;

/// <summary>
/// 用户管理控制器 - 处理用户相关操作（账号购买状态等）
/// User management controller - handles user-related operations (account purchase state, etc.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly ILogger<UserController> _logger;
    private static readonly SystemShopConfig _systemShopConfig = ConfigRepository.LoadSystemShop();

    public UserController(
        GameDbContext context,
        ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户ID
    /// Get current user ID from JWT token
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// 获取当前用户的账号购买状态 (Step 4 Phase 2)
    /// Get current user's account purchase state
    /// </summary>
    [HttpGet("purchase-state")]
    public async Task<ActionResult<AccountPurchaseStateResponse>> GetAccountPurchaseState()
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new AccountPurchaseStateResponse
            {
                Success = false,
                Message = "用户不存在"
            });
        }

        return Ok(new AccountPurchaseStateResponse
        {
            Success = true,
            Message = "获取成功",
            AccountPurchaseState = user.AccountPurchaseState
        });
    }

    /// <summary>
    /// 更新当前用户的账号购买状态 (Step 4 Phase 2)
    /// Update current user's account purchase state
    /// </summary>
    [HttpPut("purchase-state")]
    public async Task<ActionResult<AccountPurchaseStateResponse>> UpdateAccountPurchaseState(
        [FromBody] UpdateAccountPurchaseStateRequest request)
    {
        var userId = GetCurrentUserId();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new AccountPurchaseStateResponse
            {
                Success = false,
                Message = "用户不存在"
            });
        }

        if (request.AccountPurchaseState != null)
        {
            user.AccountPurchaseState = request.AccountPurchaseState;
            // 显式标记为已修改，确保 EF Core 检测到变更
            _context.Entry(user).Property(u => u.AccountPurchaseState).IsModified = true;
        }

        try
        {
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated account purchase state", userId);

            return Ok(new AccountPurchaseStateResponse
            {
                Success = true,
                Message = "账号购买状态更新成功",
                AccountPurchaseState = user.AccountPurchaseState
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update account purchase state for user {UserId}", userId);
            return StatusCode(500, new AccountPurchaseStateResponse
            {
                Success = false,
                Message = "更新账号购买状态失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 购买角色槽位 (Step 4 Phase 3)
    /// Purchase character slot
    /// </summary>
    [HttpPost("purchase-character-slot")]
    public async Task<ActionResult<PurchaseCharacterSlotResponse>> PurchaseCharacterSlot(
        [FromBody] PurchaseCharacterSlotRequest request)
    {
        var userId = GetCurrentUserId();

        // 使用事务确保并发安全
        // Use transaction to ensure concurrency safety
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new PurchaseCharacterSlotResponse
                {
                    Success = false,
                    Message = "用户不存在"
                });
            }

            // 从配置获取槽位商品信息
            var slotConfig = _systemShopConfig.GetCharacterSlotConfig();
            if (slotConfig == null)
            {
                return BadRequest(new PurchaseCharacterSlotResponse
                {
                    Success = false,
                    Message = "角色槽位商品未配置"
                });
            }

            int maxPurchases = slotConfig.Limits?.PerAccount ?? 1;
            int price = slotConfig.Price;

            // 检查是否已经购买过（账号限购）
            if (user.AccountPurchaseState.GetCount(ItemIds.CharacterSlot) >= maxPurchases)
            {
                return BadRequest(new PurchaseCharacterSlotResponse
                {
                    Success = false,
                    Message = $"您已购买过角色槽位，每个账号只能购买 {maxPurchases} 次"
                });
            }

            // 获取角色并验证金币
            var character = await _context.Characters.FirstOrDefaultAsync(
                c => c.Id == request.CharacterId && c.UserId == userId);
            if (character == null)
            {
                return NotFound(new PurchaseCharacterSlotResponse
                {
                    Success = false,
                    Message = "角色不存在"
                });
            }

            // 验证 Inventory 不为 null
            if (character.Inventory == null)
            {
                return BadRequest(new PurchaseCharacterSlotResponse
                {
                    Success = false,
                    Message = "角色背包数据异常"
                });
            }

            // 检查金币是否足够
            int goldBalance = character.Inventory.GetItemQuantity(ItemIds.GoldCoin);
            if (goldBalance < price)
            {
                return BadRequest(new PurchaseCharacterSlotResponse
                {
                    Success = false,
                    Message = $"金币不足，需要 {price} 金币"
                });
            }

            // 扣除金币（已验证 Inventory 不为 null）
            character.Inventory.RemoveItem(ItemIds.GoldCoin, price);

            // 增加角色槽位
            user.MaxCharacterSlots += 1;

            // 记录购买
            user.AccountPurchaseState.IncrementCount(ItemIds.CharacterSlot);

            // 显式标记 AccountPurchaseState 为已修改，因为 EF Core 的值转换器不会自动检测复杂对象的内部变更
            // Explicitly mark AccountPurchaseState as modified because EF Core's value converter doesn't auto-detect internal changes to complex objects
            _context.Entry(user).Property(u => u.AccountPurchaseState).IsModified = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "User {UserId} purchased character slot with character {CharacterId}. New slot count: {SlotCount}", 
                userId, request.CharacterId, user.MaxCharacterSlots);

            return Ok(new PurchaseCharacterSlotResponse
            {
                Success = true,
                Message = $"购买成功！角色槽位已增加到 {user.MaxCharacterSlots}",
                NewMaxCharacterSlots = user.MaxCharacterSlots
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to purchase character slot for user {UserId}", userId);
            return StatusCode(500, new PurchaseCharacterSlotResponse
            {
                Success = false,
                Message = "购买失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 获取角色槽位商店信息 (Step 4 Phase 3)
    /// Get character slot shop info
    /// </summary>
    [HttpGet("character-slot-shop")]
    public async Task<ActionResult<CharacterSlotShopInfo>> GetCharacterSlotShopInfo()
    {
        var userId = GetCurrentUserId();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new CharacterSlotShopInfo
            {
                Price = 0,
                MaxPurchases = 0,
                PurchasedCount = 0,
                CanPurchase = false,
                CurrentSlots = 0
            });
        }

        // 从配置获取槽位商品信息
        var slotConfig = _systemShopConfig.GetCharacterSlotConfig();
        int price = slotConfig?.Price ?? 5000;
        int maxPurchases = slotConfig?.Limits?.PerAccount ?? 1;

        int purchasedCount = user.AccountPurchaseState.GetCount(ItemIds.CharacterSlot);

        return Ok(new CharacterSlotShopInfo
        {
            Price = price,
            MaxPurchases = maxPurchases,
            PurchasedCount = purchasedCount,
            CanPurchase = purchasedCount < maxPurchases,
            CurrentSlots = user.MaxCharacterSlots
        });
    }
}
