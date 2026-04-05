using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Security.Application.Commands;

/// <summary>
/// Kullanıcının girdiği TOTP kodunu doğrular ve 2FA'yı etkinleştirir.
/// </summary>
public record Verify2FASetupCommand(string TotpCode) : IRequest<bool>;

public class Verify2FASetupCommandHandler : IRequestHandler<Verify2FASetupCommand, bool>
{
    private readonly AppDbContext       _db;
    private readonly ICurrentUser       _user;
    private readonly IEncryptionService _encryption;

    public Verify2FASetupCommandHandler(AppDbContext db, ICurrentUser user, IEncryptionService encryption)
    {
        _db         = db;
        _user       = user;
        _encryption = encryption;
    }

    public async Task<bool> Handle(Verify2FASetupCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsAuthenticated)
            throw new UnauthorizedException("Giriş yapmanız gerekiyor.");

        var settings = await _db.User2FASettings
            .FirstOrDefaultAsync(s => s.UserId == _user.UserId, cancellationToken)
            ?? throw new InvalidOperationException("2FA kurulumu başlatılmamış. Önce /api/security/2fa/setup çağrısı yapın.");

        if (string.IsNullOrEmpty(settings.TotpSecret))
            throw new InvalidOperationException("2FA kurulumu başlatılmamış.");

        var secret = _encryption.Decrypt(settings.TotpSecret);

        if (!VerifyTotp(secret, request.TotpCode))
            throw new InvalidOperationException("Geçersiz doğrulama kodu. Lütfen tekrar deneyin.");

        // 8 adet tek kullanımlık yedek kod üret
        var backupCodes = GenerateBackupCodes(8);
        var backupJson  = JsonSerializer.Serialize(backupCodes);

        settings.EnableTotp(backupJson);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ── TOTP Doğrulama (RFC 6238) ─────────────────────────────────────────

    private static bool VerifyTotp(string base32Secret, string code)
    {
        if (!int.TryParse(code, out var codeInt)) return false;

        var key  = FromBase32(base32Secret);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // ±1 adım tolerans (clock skew)
        for (long i = step - 1; i <= step + 1; i++)
        {
            if (GenerateTotp(key, i) == codeInt) return true;
        }
        return false;
    }

    private static int GenerateTotp(byte[] key, long counter)
    {
        var msg = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(msg);

        using var hmac = new HMACSHA1(key);
        var hash   = hmac.ComputeHash(msg);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   |  (hash[offset + 3] & 0xFF);
        return binary % 1_000_000;
    }

    private static byte[] FromBase32(string base32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.ToUpperInvariant().TrimEnd('=');
        var output = new List<byte>();
        int buffer = 0, bitsLeft = 0;
        foreach (var c in base32)
        {
            var idx = chars.IndexOf(c);
            if (idx < 0) continue;
            buffer  = (buffer << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8) { output.Add((byte)(buffer >> (bitsLeft - 8))); bitsLeft -= 8; }
        }
        return output.ToArray();
    }

    private static string[] GenerateBackupCodes(int count)
    {
        var codes = new string[count];
        for (int i = 0; i < count; i++)
            codes[i] = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLower();
        return codes;
    }
}
