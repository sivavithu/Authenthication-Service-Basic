using System.Threading.Tasks;

namespace OAuthAuthService.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otp, string userName);
    }
}