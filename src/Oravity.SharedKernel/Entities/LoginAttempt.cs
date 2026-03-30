namespace Oravity.SharedKernel.Entities;

public class LoginAttempt
{
    public long Id { get; private set; }
    public string Identifier { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public bool Success { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private LoginAttempt() { }

    public static LoginAttempt Create(string identifier, string? ipAddress, bool success)
    {
        return new LoginAttempt
        {
            Identifier = identifier,
            IpAddress = ipAddress,
            Success = success,
            CreatedAt = DateTime.UtcNow
        };
    }
}
