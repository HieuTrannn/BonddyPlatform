using BonddyPlatform.API.Options;
using BonddyPlatform.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BonddyPlatform.API.Services;

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public EmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.UserName))
            return; // SMTP not configured, skip sending (e.g. dev without SMTP)

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        var secureSocketOptions = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions);
        await client.AuthenticateAsync(_options.UserName, _options.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
