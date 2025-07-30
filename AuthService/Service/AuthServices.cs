using AuthService.Data;
using AuthService.Entities;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Service
{
    public class AuthServices : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthServices(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<TokenResponseDto?> LoginAsync(UserDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user is null) return null;

            var result = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed) return null;

            return await CreateTokenResponse(user);
        }

        public async Task<User?> RegisterAsync(UserDto request)
        {
            if (await _context.Users.AnyAsync(i => i.Username == request.Username)) return null;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Role = "User"
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null) return null;

            return await CreateTokenResponse(user);
        }

        public async Task<bool> RevokeRefreshTokenAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(user.RefreshTokenHash) || user.RefreshTokenExpiryTime <= DateTime.UtcNow || user.RevokedOn != null)
            {
                return true; 
            }

            user.RefreshTokenHash = null; 
            user.RefreshTokenExpiryTime = DateTime.MinValue; 
            user.RevokedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

  
        private async Task<TokenResponseDto> CreateTokenResponse(User user)
        {
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AppSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration["AppSettings:Issuer"],
                audience: _configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            var refreshTokenHash = new PasswordHasher<User>().HashPassword(user, refreshToken);

            user.RefreshTokenHash = refreshTokenHash;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.RevokedOn = null; 
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(user.RefreshTokenHash) ||
                new PasswordHasher<User>().VerifyHashedPassword(user, user.RefreshTokenHash, refreshToken) == PasswordVerificationResult.Failed)
            {
                return null; 
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null; 
            }

            if (user.RevokedOn != null) 
            {
                return null;
            }

            return user;
        }
    }
}