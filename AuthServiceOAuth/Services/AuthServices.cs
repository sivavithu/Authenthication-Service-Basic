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
        private readonly ILogger<AuthServices> _logger;
        private readonly IEmailService _emailService;

        public AuthServices(ApplicationDbContext context, IConfiguration configuration,
            ILogger<AuthServices> logger, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<TokenResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            var username = await GenerateUniqueUsernameAsync(email);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
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
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["AppSettings:RefreshTokenExpiryDays"] ?? "7"));
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
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || user.AuthProvider != "Local" || user.PasswordHash == null)
            {
                throw new UnauthorizedAccessException("Login failed. Please check your email or password.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Login failed. Please check your email or password.");
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["AppSettings:RefreshTokenExpiryDays"] ?? "7"));
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
                    Audience = new[] { _configuration["GoogleSettings:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                if (payload == null)
                {
                    throw new UnauthorizedAccessException("Invalid Google token");
                }

                var email = payload.Email.Trim().ToLowerInvariant();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject ||
                                            (u.Email == email && u.Email != null));

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = await GenerateUniqueUsernameAsync(email),
                        Email = email,
                        GoogleId = payload.Subject,
                        ProfilePicture = payload.Picture,
                        Role = "User",
                        AuthProvider = "Google",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                }
                else
                {
                    if (string.IsNullOrEmpty(user.GoogleId))
                        user.GoogleId = payload.Subject;

                    if (user.AuthProvider == "Local")
                        user.AuthProvider = "Google";

                    user.ProfilePicture = payload.Picture;
                    user.LastLoginAt = DateTime.UtcNow;
                }

                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["AppSettings:RefreshTokenExpiryDays"] ?? "7"));
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

        public async Task<bool> SendPasswordResetOtpAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || user.AuthProvider != "Local")
            {
                return true; // Don't reveal user existence
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            user.PasswordResetOtp = otp;
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(10);
            user.PasswordResetAttempts = 0;

            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(email, otp, user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP to {Email}", email);
                throw new ApplicationException("Failed to send OTP email. Please try again later.");
            }

            return true;
        }

        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            email = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || user.AuthProvider != "Local")
                throw new InvalidOperationException("Invalid verification request");

            if (string.IsNullOrEmpty(user.PasswordResetOtp))
                throw new InvalidOperationException("No OTP request found. Please request a new OTP.");

            if (user.PasswordResetOtpExpiry < DateTime.UtcNow)
            {
                user.PasswordResetOtp = null;
                user.PasswordResetOtpExpiry = null;
                user.PasswordResetAttempts = 0;
                await _context.SaveChangesAsync();

                throw new UnauthorizedAccessException("OTP has expired. Please request a new one.");
            }

            if (user.PasswordResetAttempts >= 5)
            {
                user.PasswordResetOtp = null;
                user.PasswordResetOtpExpiry = null;
                user.PasswordResetAttempts = 0;
                await _context.SaveChangesAsync();

                throw new UnauthorizedAccessException("Too many failed attempts. Please request a new OTP.");
            }

            if (user.PasswordResetOtp != otp)
            {
                user.PasswordResetAttempts++;
                await _context.SaveChangesAsync();

                throw new UnauthorizedAccessException($"Invalid OTP. {5 - user.PasswordResetAttempts} attempts remaining.");
            }

            return true;
        }

        public async Task<bool> ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
        {
            await VerifyOtpAsync(email, otp);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
                throw new InvalidOperationException("User not found");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;
            user.PasswordResetAttempts = 0;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);

            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User not found");

            if (user.RefreshToken != request.RefreshToken)
                throw new UnauthorizedAccessException("Invalid refresh token");

            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token expired");

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["AppSettings:RefreshTokenExpiryDays"] ?? "7"));
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
                throw new KeyNotFoundException("User not found");

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
                throw new KeyNotFoundException("User not found");

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
                throw new KeyNotFoundException("User not found");

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (user.AuthProvider != "Local" || user.PasswordHash == null)
                throw new InvalidOperationException("Cannot change password for OAuth users");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect");

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

            var expiresInHours = int.Parse(_configuration["AppSettings:AccessTokenExpiryHours"] ?? "24");

            var token = new JwtSecurityToken(
                issuer: _configuration["AppSettings:Issuer"],
                audience: _configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiresInHours),
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
