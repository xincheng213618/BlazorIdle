using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorIdle.Server.Data;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Shared.Models;
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

    // 角色槽位价格
    private const int CharacterSlotPrice = 5000;

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

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new PurchaseCharacterSlotResponse
            {
                Success = false,
                Message = "用户不存在"
            });
        }

        // 检查是否已经购买过（账号限购1次）
        const string slotItemId = "character_slot";
        if (user.AccountPurchaseState.GetCount(slotItemId) >= 1)
        {
            return BadRequest(new PurchaseCharacterSlotResponse
            {
                Success = false,
                Message = "您已购买过角色槽位，每个账号只能购买一次"
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

        // 检查金币是否足够
        int goldBalance = character.Inventory?.GetItemQuantity("gold_coin") ?? 0;
        if (goldBalance < CharacterSlotPrice)
        {
            return BadRequest(new PurchaseCharacterSlotResponse
            {
                Success = false,
                Message = $"金币不足，需要 {CharacterSlotPrice} 金币"
            });
        }

        try
        {
            // 扣除金币
            character.Inventory?.RemoveItem("gold_coin", CharacterSlotPrice);

            // 增加角色槽位
            user.MaxCharacterSlots += 1;

            // 记录购买
            user.AccountPurchaseState.IncrementCount(slotItemId);

            await _context.SaveChangesAsync();

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
            return NotFound();
        }

        const string slotItemId = "character_slot";
        int purchasedCount = user.AccountPurchaseState.GetCount(slotItemId);

        return Ok(new CharacterSlotShopInfo
        {
            Price = CharacterSlotPrice,
            MaxPurchases = 1,
            PurchasedCount = purchasedCount,
            CanPurchase = purchasedCount < 1,
            CurrentSlots = user.MaxCharacterSlots
        });
    }
}
