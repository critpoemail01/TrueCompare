using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class SmtpEmailSender(IOptions<EmailOptions> optionsAccessor, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions options = optionsAccessor.Value;

    public async Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.FromEmail))
        {
            logger.LogWarning("SMTP email is not configured. Email to {Email} with subject {Subject} was skipped.", to, subject);
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(options.FromEmail, options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(options.Host, options.Port)
            {
                EnableSsl = options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                client.Credentials = new NetworkCredential(options.UserName, options.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "SMTP email to {Email} with subject {Subject} failed.", to, subject);
            return false;
        }
    }
}
