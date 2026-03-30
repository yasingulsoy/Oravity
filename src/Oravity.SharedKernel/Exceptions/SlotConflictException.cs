namespace Oravity.SharedKernel.Exceptions;

/// <summary>
/// Randevu slot çakışması — HTTP 409 Conflict döner.
/// Aynı doktor + branch + zaman diliminde başka randevu var.
/// </summary>
public class SlotConflictException : AppException
{
    public SlotConflictException(
        string message = "Bu slot başka bir kullanıcı tarafından alındı.")
        : base(message, 409) { }
}
