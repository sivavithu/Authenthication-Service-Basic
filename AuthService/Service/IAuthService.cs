using AuthService.Models;
using System.Threading.Tasks;
using AuthService.Entities;

namespace AuthService.Service
{
    public interface IAuthService
    {
        Task<TokenResponseDto?> LoginAsync(UserDto request);
        Task<User?> RegisterAsync(UserDto request);
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
    }
}