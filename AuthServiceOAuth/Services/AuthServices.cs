using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthService.Data;
using OAuthAuthService.Entities;
using OAuthAuthService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OAuthAuthService.Services
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

        public async Task<TokenResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            var username = await GenerateUniqueUsernameAsync(request.Email);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User",
                AuthProvider = "Local",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Role = user.Role
            };
        }

        public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null || user.AuthProvider != "Local" || user.PasswordHash == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Role = user.Role
            };
        }

        public async Task<TokenResponseDto> GoogleAuthAsync(GoogleAuthRequestDto request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                if (payload == null)
                {
                    throw new UnauthorizedAccessException("Invalid Google token");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject ||
                                            (u.Email == payload.Email && u.Email != null));

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = await GenerateUniqueUsernameAsync(payload.Email),
                        Email = payload.Email,
                        GoogleId = payload.Subject,
                        ProfilePicture = payload.Picture,
                        Role = "User",
                        AuthProvider = "Google",
                        PasswordHash = null,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                }
                else
                {
                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = payload.Subject;
                    }
                    if (user.AuthProvider == "Local")
                    {
                        user.AuthProvider = "Google";
                    }
                    user.ProfilePicture = payload.Picture;
                    user.LastLoginAt = DateTime.UtcNow;
                }

                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    ProfilePicture = user.ProfilePicture,
                    Role = user.Role
                };
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException("Invalid Google token");
            }
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            if (user.RefreshToken != request.RefreshToken)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token expired");
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Role = user.Role
            };
        }

        public async Task<UserInfoDto> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                AuthProvider = user.AuthProvider,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };
        }

        public async Task<List<UserInfoDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return users.Select(u => new UserInfoDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                AuthProvider = u.AuthProvider,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive
            }).ToList();
        }

        public async Task<UserInfoDto> UpdateUserRoleAsync(Guid userId, string role)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            user.Role = role;
            await _context.SaveChangesAsync();

            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                AuthProvider = user.AuthProvider,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (user.AuthProvider != "Local" || user.PasswordHash == null)
            {
                throw new InvalidOperationException("Cannot change password for OAuth users");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public string GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["AppSettings:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("AuthProvider", user.AuthProvider)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["AppSettings:Issuer"],
                audience: _configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private async Task<string> GenerateUniqueUsernameAsync(string email)
        {
            var baseUsername = email.Split('@')[0];
            var username = baseUsername;
            var counter = 1;

            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }
    }
}