using OAuthAuthService.Entities;
using OAuthAuthService.Models;

namespace OAuthAuthService.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<TokenResponseDto> LoginAsync(LoginRequestDto request);
        Task<TokenResponseDto> GoogleAuthAsync(GoogleAuthRequestDto request);
        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<UserInfoDto> GetUserByIdAsync(Guid userId);
        Task<List<UserInfoDto>> GetAllUsersAsync();
        Task<UserInfoDto> UpdateUserRoleAsync(Guid userId, string role);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();

        Task<bool> SendPasswordResetOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string otp);
        Task<bool> ResetPasswordWithOtpAsync(string email, string otp, string newPassword);
    }
}