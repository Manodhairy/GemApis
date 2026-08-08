using GemApi.Data;
using GemApi.Dto.Request;
using GemApi.Dto.Response;
using GemApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
}