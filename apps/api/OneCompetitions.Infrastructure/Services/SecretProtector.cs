using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace OneCompetitions.Infrastructure.Services;

public sealed class SecretProtector(IConfiguration configuration, IHostEnvironment environment)
{
    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[Encoding.UTF8.GetByteCount(plaintext)];
        var tag = new byte[16];
        using var aes = new AesGcm(GetKey(), 16);
        aes.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);
        return Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]);
    }

    public string Unprotect(string protectedValue)
    {
        var bytes = Convert.FromBase64String(protectedValue);
        if (bytes.Length < 29) throw new InvalidOperationException("Encrypted secret is invalid.");
        var plaintext = new byte[bytes.Length - 28];
        using var aes = new AesGcm(GetKey(), 16);
        aes.Decrypt(bytes[..12], bytes[28..], bytes[12..28], plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var value = configuration["ENCRYPTION_KEY"];
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                var key = Convert.FromBase64String(value);
                if (key.Length == 32) return key;
            }
            catch (FormatException) { }
        }

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            return SHA256.HashData(Encoding.UTF8.GetBytes("one-competitions-development-encryption-key"));
        throw new InvalidOperationException("ENCRYPTION_KEY must be a base64-encoded 32-byte key.");
    }
}
