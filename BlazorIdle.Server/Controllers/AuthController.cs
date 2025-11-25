using BlazorIdle.Server.Data;
using BlazorIdle.Server.Services;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorIdle.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        GameDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
    }

    // Sanitize username for logging to prevent log forging
    private static string SanitizeForLogging(string input)
    {
        // Remove any characters that could cause log forging (newlines, carriage returns, etc.)
        return System.Text.RegularExpressions.Regex.Replace(input, @"[\r\n\t]", "");
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "用户名和密码不能为空"
                });
            }

            // Validate username format (alphanumeric and underscore only, 3-50 characters)
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]{3,50}$"))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "用户名只能包含字母、数字和下划线，长度为3-50个字符"
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "密码长度至少为6个字符"
                });
            }

            // Check if username already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "用户名已存在"
                });
            }

            // Create new user
            var user = new User
            {
                Username = request.Username,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                MaxCharacterSlots = 1  // 默认1个槽位，可通过系统商店购买更多
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Username, user.Id);

            // Safe structured logging - username is sanitized to prevent log forging
            _logger.LogInformation("User registered successfully: {Username}", SanitizeForLogging(user.Username));

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "注册成功",
                Token = token,
                Username = user.Username
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "注册过程中发生错误"
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "用户名和密码不能为空"
                });
            }

            // Validate username format to prevent log forging
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]{1,50}$"))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            // Find user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Username, user.Id);

            // Safe structured logging - username is sanitized to prevent log forging
            _logger.LogInformation("User logged in successfully: {Username}", SanitizeForLogging(user.Username));

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "登录成功",
                Token = token,
                Username = user.Username
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "登录过程中发生错误"
            });
        }
    }
}
