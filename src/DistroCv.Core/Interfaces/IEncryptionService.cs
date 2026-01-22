namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
