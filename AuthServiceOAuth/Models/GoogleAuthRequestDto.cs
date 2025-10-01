using System.ComponentModel.DataAnnotations;

namespace OAuthAuthService.Models
{
    public class GoogleAuthRequestDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class UpdateRoleRequestDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [RegularExpression("^(User|Admin)$", ErrorMessage = "Role must be either 'User' or 'Admin'")]
        public string Role { get; set; } = string.Empty;
    }
}