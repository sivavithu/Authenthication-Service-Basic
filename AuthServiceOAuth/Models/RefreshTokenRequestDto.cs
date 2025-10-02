using System;
using System.ComponentModel.DataAnnotations;

namespace OAuthAuthService.Models
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}