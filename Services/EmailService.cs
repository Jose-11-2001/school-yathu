// Services/EmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using School_Yathu.DTOs;
using School_Yathu.Settings;

namespace School_Yathu.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, 
                    _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent to {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {to}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationEmailAsync(NotificationEmailDTO dto)
        {
            var subject = dto.Subject ?? "New Notification from Maranatha Secondary School";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #1a365d, #2b6cb0); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
                        .header h1 {{ margin: 0; font-size: 24px; }}
                        .content {{ background: #f7fafc; padding: 30px; border-left: 1px solid #e2e8f0; border-right: 1px solid #e2e8f0; }}
                        .footer {{ background: #edf2f7; padding: 15px; text-align: center; font-size: 12px; color: #718096; border-radius: 0 0 8px 8px; border: 1px solid #e2e8f0; }}
                        .button {{ background: #2b6cb0; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .notification {{ background: #ebf8ff; padding: 15px; border-left: 4px solid #2b6cb0; margin: 20px 0; }}
                        .notification-type {{ display: inline-block; padding: 3px 10px; border-radius: 12px; font-size: 12px; font-weight: bold; }}
                        .type-result {{ background: #48bb78; color: white; }}
                        .type-deadline {{ background: #ed8936; color: white; }}
                        .type-enrollment {{ background: #4299e1; color: white; }}
                        .type-general {{ background: #9f7aea; color: white; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>📚 Maranatha Secondary School</h1>
                            <p style='margin: 5px 0 0; opacity: 0.9;'>Official Notification</p>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{dto.UserName}</strong>,</p>
                            
                            <div class='notification'>
                                <span class='notification-type type-{dto.Type?.ToLower() ?? "general"}'>
                                    {dto.Type?.ToUpper() ?? "GENERAL"}
                                </span>
                                <p style='margin-top: 10px; font-size: 16px;'>{dto.Message}</p>
                            </div>
                            
                            <p style='font-size: 14px; color: #4a5568;'>
                                Please log in to the school portal to view more details.
                            </p>
                            
                            <p style='text-align: center; margin: 25px 0;'>
                                <a href='{dto.Link ?? "https://your-school-portal.com"}' class='button'>
                                    View in Portal
                                </a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} Maranatha Secondary School. All rights reserved.</p>
                            <p>This is an automated notification. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(dto.UserEmail, subject, body, true);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string to, string name, string newPassword)
        {
            var subject = "Password Reset - Maranatha Secondary School";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #1a365d, #2b6cb0); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f7fafc; padding: 30px; border-left: 1px solid #e2e8f0; border-right: 1px solid #e2e8f0; }}
                        .footer {{ background: #edf2f7; padding: 15px; text-align: center; font-size: 12px; color: #718096; border-radius: 0 0 8px 8px; border: 1px solid #e2e8f0; }}
                        .password-box {{ background: #ebf8ff; padding: 15px; border: 2px dashed #2b6cb0; border-radius: 8px; text-align: center; font-size: 18px; font-weight: bold; margin: 20px 0; }}
                        .warning {{ background: #fefcbf; padding: 15px; border-radius: 5px; border-left: 4px solid #ed8936; margin: 20px 0; font-size: 14px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔐 Password Reset</h1>
                            <p style='margin: 5px 0 0; opacity: 0.9;'>Maranatha Secondary School</p>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{name}</strong>,</p>
                            <p>Your password has been reset successfully.</p>
                            
                            <div class='password-box'>
                                New Password: <span style='color: #2b6cb0;'>{newPassword}</span>
                            </div>
                            
                            <div class='warning'>
                                ⚠️ <strong>Important:</strong> Please change this password after logging in.
                            </div>
                            
                            <p>You can log in to the portal using:</p>
                            <ul>
                                <li><strong>Email:</strong> {to}</li>
                                <li><strong>Password:</strong> {newPassword}</li>
                            </ul>
                            
                            <p style='text-align: center; margin: 25px 0;'>
                                <a href='https://your-school-portal.com/login' style='background: #2b6cb0; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Login to Portal
                                </a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} Maranatha Secondary School. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string name, string role, string admissionNumber = null)
        {
            var subject = $"Welcome to Maranatha Secondary School - {role} Account Created";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #1a365d, #2b6cb0); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f7fafc; padding: 30px; border-left: 1px solid #e2e8f0; border-right: 1px solid #e2e8f0; }}
                        .footer {{ background: #edf2f7; padding: 15px; text-align: center; font-size: 12px; color: #718096; border-radius: 0 0 8px 8px; border: 1px solid #e2e8f0; }}
                        .info-box {{ background: #ebf8ff; padding: 15px; border-radius: 5px; margin: 10px 0; }}
                        .warning {{ background: #fefcbf; padding: 15px; border-radius: 5px; border-left: 4px solid #ed8936; margin: 20px 0; font-size: 14px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎓 Welcome to Maranatha Secondary School</h1>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{name}</strong>,</p>
                            <p>Your {role} account has been created successfully.</p>
                            
                            <div class='info-box'>
                                <h3 style='margin-top: 0;'>Account Details:</h3>
                                <p><strong>Role:</strong> {role}</p>
                                {(admissionNumber != null ? $"<p><strong>Admission Number:</strong> {admissionNumber}</p>" : "")}
                                <p><strong>Email:</strong> {to}</p>
                            </div>
                            
                            <div class='warning'>
                                ⚠️ <strong>Important:</strong> Please change your password after first login.
                            </div>
                            
                            <p style='text-align: center; margin: 25px 0;'>
                                <a href='https://your-school-portal.com/login' style='background: #2b6cb0; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Login to Portal
                                </a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} Maranatha Secondary School. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }
    }
}