namespace Oravity.SharedKernel.Interfaces;

public interface IEncryptionService
{
    /// <summary>AES-256-CBC ile şifreler. Dönen değer Base64 (IV + ciphertext).</summary>
    string Encrypt(string plaintext);

    /// <summary>Şifrelenmiş Base64 değeri çözer.</summary>
    string Decrypt(string ciphertext);

    /// <summary>SHA-256 hex hash — arama indexi için.</summary>
    string HashSha256(string value);
}
