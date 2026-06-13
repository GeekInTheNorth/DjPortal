using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace DjPortalApi.Features.Contact;

public sealed class EmailService : IEmailService
{
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly string? _fromAddress;
    private readonly string? _recipient;

    public EmailService(IConfiguration configuration)
    {
        _smtpHost = configuration.GetValue<string>("SmtpHost");
        _smtpPort = configuration.GetValue<int?>("SmtpPort") ?? 587;
        _smtpUsername = configuration.GetValue<string>("SmtpUsername");
        _smtpPassword = configuration.GetValue<string>("SmtpPassword");
        _fromAddress = configuration.GetValue<string>("ContactFromAddress");
        _recipient = configuration.GetValue<string>("ContactRecipient");
    }

    public async Task SendContactEmailAsync(ContactModel model)
    {
        if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUsername) ||
            string.IsNullOrWhiteSpace(_smtpPassword) || string.IsNullOrWhiteSpace(_fromAddress) ||
            string.IsNullOrWhiteSpace(_recipient))
        {
            throw new InvalidOperationException("Email service is not configured.");
        }

        var message = new MimeMessage();
        // Gmail requires the From address to be the authenticated account.
        message.From.Add(MailboxAddress.Parse(_fromAddress));
        message.To.Add(MailboxAddress.Parse(_recipient));
        // Reply-To is the visitor, so replies go straight back to them.
        message.ReplyTo.Add(new MailboxAddress(model.Name, model.Email));
        message.Subject = $"Contact: {model.Subject}";
        message.Body = new TextPart("plain")
        {
            Text = $"Name: {model.Name}\n" +
                   $"Email: {model.Email}\n\n" +
                   $"{model.Message}"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
