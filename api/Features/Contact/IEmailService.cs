namespace DjPortalApi.Features.Contact;

public interface IEmailService
{
    Task SendContactEmailAsync(ContactModel model);
}
