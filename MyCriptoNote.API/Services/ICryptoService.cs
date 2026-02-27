namespace MyCriptoNote.API.Services;

public record EncryptionResult(string EncryptedContent, string Salt, string IV, string AuthTag);

public interface ICryptoService
{
    EncryptionResult Encrypt(string content, string password, string? salt = null);
    string Decrypt(string encryptedContent, string password, string salt, string iv, string? authTag = null);
    string GenerateSalt();
}
