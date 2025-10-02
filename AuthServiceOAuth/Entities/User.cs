using System;
using System.ComponentModel.DataAnnotations;

namespace OAuthAuthService.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Email { get; set; }

        // For traditional auth - null for OAuth users
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "User";

        // Refresh token management
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // OAuth provider information
        [Required]
        [MaxLength(20)]
        public string AuthProvider { get; set; } = "Local"; // "Local" or "Google"

        [MaxLength(255)]
        public string? GoogleId { get; set; }

        [MaxLength(500)]
        public string? ProfilePicture { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
        public int PasswordResetAttempts { get; set; } = 0;


    }
}