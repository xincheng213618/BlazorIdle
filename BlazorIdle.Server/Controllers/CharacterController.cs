using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorIdle.Server.Data;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Shared.Models;
using System.Security.Claims;
using BlazorIdle.Game.Config;
using Microsoft.Extensions.Configuration;

namespace BlazorIdle.Server.Controllers;

/// <summary>
/// 角色管理控制器 - 处理角色的创建、读取、删除等操作
/// Character management controller - handles character creation, reading, deletion, etc.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CharacterController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CharacterController> _logger;

    public CharacterController(
        GameDbContext context,
        IConfiguration configuration,
        ILogger<CharacterController> logger)
    {
        _context = context;
        _configuration = configuration;
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
    /// 获取当前用户的所有角色
    /// Get all characters for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CharacterListResponse>> GetCharacters()
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        var characters = await _context.Characters
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(new CharacterListResponse
        {
            Characters = characters,
            MaxSlots = user.MaxCharacterSlots,
            UsedSlots = user.UsedCharacterSlots
        });
    }

    /// <summary>
    /// 根据ID获取指定角色
    /// Get a specific character by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CharacterResponse>> GetCharacter(string id)
    {
        var userId = GetCurrentUserId();
        
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (character == null)
        {
            return NotFound(new CharacterResponse
            {
                Success = false,
                Message = "角色不存在或无权访问"
            });
        }

        return Ok(new CharacterResponse
        {
            Success = true,
            Message = "获取成功",
            Character = character
        });
    }

    /// <summary>
    /// 创建新角色
    /// Create a new character
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CharacterResponse>> CreateCharacter([FromBody] CreateCharacterRequest request)
    {
        var userId = GetCurrentUserId();
        
        // 获取用户信息
        // Get user information
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new CharacterResponse
            {
                Success = false,
                Message = "用户不存在"
            });
        }

        // 检查角色槽位限制
        // Check character slot limit
        if (user.UsedCharacterSlots >= user.MaxCharacterSlots)
        {
            return BadRequest(new CharacterResponse
            {
                Success = false,
                Message = $"已达到最大角色数量限制（{user.MaxCharacterSlots}个）"
            });
        }

        // 从配置文件获取角色名称长度限制
        // Get character name length limits from configuration
        var minLength = _configuration.GetValue<int>("CharacterConfig:characterNameMinLength", 2);
        var maxLength = _configuration.GetValue<int>("CharacterConfig:characterNameMaxLength", 20);

        // 验证角色名称
        // Validate character name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new CharacterResponse
            {
                Success = false,
                Message = "角色名称不能为空"
            });
        }

        if (request.Name.Length < minLength || request.Name.Length > maxLength)
        {
            return BadRequest(new CharacterResponse
            {
                Success = false,
                Message = $"角色名称长度必须在{minLength}-{maxLength}个字符之间"
            });
        }

        // 检查同一用户下是否已存在同名角色
        // Check if character with same name already exists for this user
        var existingCharacter = await _context.Characters
            .AnyAsync(c => c.UserId == userId && c.Name == request.Name.Trim());
        
        if (existingCharacter)
        {
            return BadRequest(new CharacterResponse
            {
                Success = false,
                Message = "该角色名称已存在"
            });
        }

        // 验证职业ID（这里应该验证职业是否存在于配置中）
        // Validate profession ID (should verify if profession exists in config)
        if (string.IsNullOrWhiteSpace(request.ProfessionId))
        {
            return BadRequest(new CharacterResponse
            {
                Success = false,
                Message = "必须选择一个职业"
            });
        }

        // 从配置文件加载职业数据进行验证
        // Load profession data from config file for validation
        ProfessionDef? profession = null;
        try
        {
            var professionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "professions.json");
            if (System.IO.File.Exists(professionsPath))
            {
                var professionsJson = await System.IO.File.ReadAllTextAsync(professionsPath);
                var professions = System.Text.Json.JsonSerializer.Deserialize<List<ProfessionDef>>(professionsJson);
                profession = professions?.FirstOrDefault(p => p.Id == request.ProfessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profession config");
        }

        if (profession == null)
        {
            return BadRequest(new CharacterResponse
            {
                Success = false,
                Message = "无效的职业选择"
            });
        }

        // 创建角色实例
        // Create character instance
        var character = new CharacterData
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = request.Name.Trim(),
            ProfessionId = request.ProfessionId,
            CreatedAt = DateTime.UtcNow,
            // 从职业模板复制初始属性
            // Copy initial stats from profession template
            MaxHp = profession.MaxHp,
            AttackRateAPS = profession.AttackRateAPS,
            DamagePerAttack = profession.DamagePerAttack,
            HastePercent = profession.HastePercent,
            SpecialIntervalSec = profession.SpecialIntervalSec,
            SpecialDamage = profession.SpecialDamage,
            CritChancePercent = profession.CritChancePercent,
            CritMultiplier = profession.CritMultiplier,
            VariancePct = profession.VariancePct,
            ReviveSec = profession.ReviveSec
        };

        // 保存角色到数据库
        // Save character to database
        _context.Characters.Add(character);
        
        // 更新用户已使用的角色槽位数
        // Update user's used character slots count
        user.UsedCharacterSlots++;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} created character {character.Id} ({character.Name})");

        return Ok(new CharacterResponse
        {
            Success = true,
            Message = "角色创建成功",
            Character = character
        });
    }

    /// <summary>
    /// 删除角色
    /// Delete a character
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<CharacterResponse>> DeleteCharacter(string id)
    {
        var userId = GetCurrentUserId();
        
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (character == null)
        {
            return NotFound(new CharacterResponse
            {
                Success = false,
                Message = "角色不存在或无权删除"
            });
        }

        // 删除角色
        // Delete character
        _context.Characters.Remove(character);
        
        // 更新用户已使用的角色槽位数
        // Update user's used character slots count
        var user = await _context.Users.FindAsync(userId);
        if (user != null && user.UsedCharacterSlots > 0)
        {
            user.UsedCharacterSlots--;
        }
        
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} deleted character {character.Id} ({character.Name})");

        return Ok(new CharacterResponse
        {
            Success = true,
            Message = "角色删除成功"
        });
    }
}
