using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerminalChatApp.Backend.Data;
using TerminalChatApp.Backend.Models;
using TerminalChatApp.Backend.Services;
using BCrypt.Net;

namespace TerminalChatApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ChatContext _context;
    private readonly JwtService _jwtService;

    public AuthController(ChatContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Username already exists"
                });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Email already registered"
                });
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "User registered successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Registration failed: {ex.Message}"
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            // Update user status
            user.IsOnline = true;
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    IsOnline = user.IsOnline,
                    LastSeen = user.LastSeen
                },
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { Success = true, Message = "Logout successful" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = $"Logout failed: {ex.Message}" });
        }
    }
}