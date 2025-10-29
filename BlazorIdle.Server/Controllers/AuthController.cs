using BlazorIdle.Server.Data;
using BlazorIdle.Server.Services;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Shared.Models;
using BlazorIdle.Shared.Validation;
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
            // Sanitize inputs
            request.Username = ValidationHelper.SanitizeUsername(request.Username);
            request.Password = ValidationHelper.SanitizePassword(request.Password);

            // Validate username
            var usernameValidation = ValidationHelper.ValidateUsername(request.Username);
            if (!usernameValidation.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = usernameValidation.ErrorMessage
                });
            }

            // Validate password
            var passwordValidation = ValidationHelper.ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = passwordValidation.ErrorMessage
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
                CreatedAt = DateTime.UtcNow
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
            // Sanitize inputs
            request.Username = ValidationHelper.SanitizeUsername(request.Username);
            request.Password = ValidationHelper.SanitizePassword(request.Password);

            // Validate username format
            var usernameValidation = ValidationHelper.ValidateUsername(request.Username);
            if (!usernameValidation.IsValid)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            // Validate password format
            var passwordValidation = ValidationHelper.ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
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
