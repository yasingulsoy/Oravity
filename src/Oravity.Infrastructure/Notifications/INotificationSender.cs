namespace Oravity.Infrastructure.Notifications;

public interface INotificationSender
{
    Task SendSmsAsync(string phone, string message, CancellationToken ct = default);
    Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    Task SendPushAsync(string deviceToken, string title, string body, CancellationToken ct = default);
}
