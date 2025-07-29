using AuthService.Models;
using AuthService.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AuthService.Controllers
{
    [Route("")]  // Add prefix here – all endpoints now /auth/register, /auth/login, etc.
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(UserDto request)
        {
            var user = await _authService.RegisterAsync(request);
            if (user == null) return BadRequest("User already exists");
            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(UserDto request)
        {
            var results = await _authService.LoginAsync(request);
            Console.WriteLine(results);
            if (results == null) return BadRequest("Invalid username or password");
            return Ok(results);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var results = await _authService.RefreshTokenAsync(request);
            if (results == null) return Unauthorized("Invalid refresh token");
            return Ok(results);
        }
    }
}