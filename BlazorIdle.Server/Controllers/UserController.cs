using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorIdle.Server.Data;
using BlazorIdle.Shared.DTOs;
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
}
