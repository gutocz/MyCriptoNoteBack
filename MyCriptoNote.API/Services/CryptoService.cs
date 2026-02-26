using System.Security.Cryptography;
using System.Text;
using MyCriptoNote.API.Exceptions;

namespace MyCriptoNote.API.Services;

public class CryptoService : ICryptoService
{
    private const int Iterations = 100_000;
    private const int KeySize = 32;
    private const int SaltSize = 16;
    private const int IvSize = 16;

    public EncryptionResult Encrypt(string content, string password, string? salt = null)
    {
        byte[] saltBytes = salt != null
            ? Convert.FromBase64String(salt)
            : RandomNumberGenerator.GetBytes(SaltSize);

        byte[] ivBytes = RandomNumberGenerator.GetBytes(IvSize);

        byte[] key = DeriveKey(password, saltBytes);

        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            byte[] encrypted = encryptor.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            return new EncryptionResult(
                Convert.ToBase64String(encrypted),
                Convert.ToBase64String(saltBytes),
                Convert.ToBase64String(ivBytes)
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public string Decrypt(string encryptedContent, string password, string salt, string iv)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] ivBytes = Convert.FromBase64String(iv);
        byte[] cipherBytes = Convert.FromBase64String(encryptedContent);

        byte[] key = DeriveKey(password, saltBytes);

        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
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
            CryptographicOperations.ZeroMemory(key);
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
        return pbkdf2.GetBytes(KeySize);
    }
}
