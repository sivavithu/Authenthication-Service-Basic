using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OAuthAuthService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp, string userName)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "Password Reset OTP",
                    Body = GetOtpEmailBody(otp, userName),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
                throw new InvalidOperationException("Failed to send OTP email");
            }
        }

        private string GetOtpEmailBody(string otp, string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #4285f4; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .otp-box {{ background: white; border: 2px dashed #4285f4; padding: 20px; 
                    text-align: center; font-size: 32px; font-weight: bold; 
                    letter-spacing: 8px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .warning {{ color: #d32f2f; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>You requested to reset your password. Use the OTP below to complete the process:</p>
            
            <div class='otp-box'>{otp}</div>
            
            <p><strong>This OTP is valid for 10 minutes.</strong></p>
            
            <div class='warning'>
                <p><strong>⚠️ Security Notice:</strong></p>
                <ul>
                    <li>Do not share this OTP with anyone</li>
                    <li>We will never ask for your OTP via phone or email</li>
                    <li>If you didn't request this, please ignore this email</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply.</p>
            <p>&copy; 2025 Your Application. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}