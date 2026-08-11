using GemApi.Data;
using GemApi.Dto.Request;
using GemApi.Dto.Response;
using GemApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GemApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == request.Email);

            if (admin == null)
                return Unauthorized("Invalid email or password");

            if (admin.Password != request.Password)
                return Unauthorized("Invalid email or password");

            var token = _jwtService.CreateToken(
                admin.Id.ToString(),
                admin.Name,
                new[] { admin.Role }
            );

            admin.Token = token;

            await _context.SaveChangesAsync();

            return Ok(new LoginResponseDto
            {
                Token = token,
                Name = admin.Name,
                Email = admin.Email,
                Role = admin.Role
            });
        }
        [Authorize]
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // FIX: Change int to long
            if (!long.TryParse(userIdStr, out long userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var admin = await _context.Admins.FindAsync(userId);
            if (admin == null)
            {
                return NotFound(new { message = "User not found." });
            }

            admin.Token = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Logged out successfully." });
        }
    }
}