
using School_Yathu.DTOs;

namespace School_Yathu.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task<bool> SendNotificationEmailAsync(NotificationEmailDTO dto);
        Task<bool> SendPasswordResetEmailAsync(string to, string name, string newPassword);
        Task<bool> SendWelcomeEmailAsync(string to, string name, string role, string admissionNumber = null);
    }
}