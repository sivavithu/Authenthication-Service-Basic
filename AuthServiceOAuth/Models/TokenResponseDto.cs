using System;

namespace OAuthAuthService.Models
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }
}