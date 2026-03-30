using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Services;

/// <summary>
/// AES-256-CBC tabanlı şifreleme ve SHA-256 hash servisi.
/// Key: appsettings Encryption:Key (Base64 kodlanmış 32 byte).
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key ayarı eksik.");
        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key tam olarak 32 byte (256-bit) olmalıdır.");
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // IV (16 byte) + ciphertext'i birleştir
        var result = new byte[aes.IV.Length + ciphertextBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertextBytes, 0, result, aes.IV.Length, ciphertextBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var allBytes = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;

        var ivSize = aes.BlockSize / 8;
        var iv = new byte[ivSize];
        var data = new byte[allBytes.Length - ivSize];
        Buffer.BlockCopy(allBytes, 0, iv, 0, ivSize);
        Buffer.BlockCopy(allBytes, ivSize, data, 0, data.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public string HashSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
