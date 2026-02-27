using System.Security.Cryptography;
using System.Text;
using MyCriptoNote.API.Exceptions;

namespace MyCriptoNote.API.Services;

public class CryptoService : ICryptoService
{
    private const int Iterations = 100_000;
    private const int AesKeySize = 32;
    private const int HmacKeySize = 32;
    private const int DerivedKeySize = AesKeySize + HmacKeySize;
    private const int SaltSize = 16;
    private const int IvSize = 16;

    public EncryptionResult Encrypt(string content, string password, string? salt = null)
    {
        byte[] saltBytes = salt != null
            ? Convert.FromBase64String(salt)
            : RandomNumberGenerator.GetBytes(SaltSize);

        byte[] ivBytes = RandomNumberGenerator.GetBytes(IvSize);

        byte[] derivedKey = DeriveKey(password, saltBytes);
        byte[] aesKey = derivedKey[..AesKeySize];
        byte[] hmacKey = derivedKey[AesKeySize..];

        try
        {
            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            byte[] encrypted = encryptor.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            byte[] authTag = ComputeHmac(hmacKey, ivBytes, encrypted);

            return new EncryptionResult(
                Convert.ToBase64String(encrypted),
                Convert.ToBase64String(saltBytes),
                Convert.ToBase64String(ivBytes),
                Convert.ToBase64String(authTag)
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(hmacKey);
        }
    }

    public string Decrypt(string encryptedContent, string password, string salt, string iv, string? authTag = null)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] ivBytes = Convert.FromBase64String(iv);
        byte[] cipherBytes = Convert.FromBase64String(encryptedContent);

        byte[] derivedKey = DeriveKey(password, saltBytes);
        byte[] aesKey = derivedKey[..AesKeySize];
        byte[] hmacKey = derivedKey[AesKeySize..];

        try
        {
            if (!string.IsNullOrEmpty(authTag))
            {
                byte[] expectedTag = Convert.FromBase64String(authTag);
                byte[] computedTag = ComputeHmac(hmacKey, ivBytes, cipherBytes);

                if (!CryptographicOperations.FixedTimeEquals(expectedTag, computedTag))
                    throw new InvalidPasswordException();
            }

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (CryptographicException)
        {
            throw new InvalidPasswordException();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(hmacKey);
        }
    }

    public string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256
        );
        return pbkdf2.GetBytes(DerivedKeySize);
    }

    private static byte[] ComputeHmac(byte[] hmacKey, byte[] iv, byte[] ciphertext)
    {
        using var hmac = new HMACSHA256(hmacKey);
        hmac.TransformBlock(iv, 0, iv.Length, null, 0);
        hmac.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        return hmac.Hash!;
    }
}
