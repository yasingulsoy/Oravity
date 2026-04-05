using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Security.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Security.Application.Commands;

/// <summary>
/// TOTP kurulum başlatır: secret üretir, kaydeder (henüz etkinleştirmez),
/// QR kod için otpauth URI döner.
/// </summary>
public record Setup2FACommand : IRequest<Setup2FAResponse>;

public class Setup2FACommandHandler : IRequestHandler<Setup2FACommand, Setup2FAResponse>
{
    private readonly AppDbContext       _db;
    private readonly ICurrentUser       _user;
    private readonly IEncryptionService _encryption;

    public Setup2FACommandHandler(AppDbContext db, ICurrentUser user, IEncryptionService encryption)
    {
        _db         = db;
        _user       = user;
        _encryption = encryption;
    }

    public async Task<Setup2FAResponse> Handle(Setup2FACommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsAuthenticated)
            throw new UnauthorizedException("Giriş yapmanız gerekiyor.");

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _user.UserId, cancellationToken)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        // 20 byte = 160 bit random secret (RFC 6238 önerir ≥128 bit)
        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var secretBase32 = ToBase32(secretBytes);

        var settings = await _db.User2FASettings
            .FirstOrDefaultAsync(s => s.UserId == _user.UserId, cancellationToken);

        if (settings is null)
        {
            settings = User2FASettings.CreateDefault(_user.UserId);
            _db.User2FASettings.Add(settings);
        }

        settings.SetupTotp(_encryption.Encrypt(secretBase32));
        await _db.SaveChangesAsync(cancellationToken);

        var issuer  = "Oravity";
        var account = Uri.EscapeDataString(user.Email);
        var uri     = $"otpauth://totp/{issuer}:{account}?secret={secretBase32}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

        return new Setup2FAResponse(uri, secretBase32);
    }

    // ── Base32 encode (RFC 4648) ───────────────────────────────────────────
    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    private static string ToBase32(byte[] data)
    {
        var result = new System.Text.StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = data[0], bitsLeft = 8;
        for (int i = 1; i < data.Length || bitsLeft > 0;)
        {
            if (bitsLeft < 5)
            {
                if (i < data.Length) { buffer = (buffer << 8) | data[i++]; bitsLeft += 8; }
                else { buffer <<= 5 - bitsLeft; bitsLeft = 5; }
            }
            result.Append(Base32Chars[(buffer >> (bitsLeft - 5)) & 31]);
            bitsLeft -= 5;
        }
        return result.ToString();
    }
}
