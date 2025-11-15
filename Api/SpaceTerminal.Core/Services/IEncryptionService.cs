namespace SpaceTerminal.Core.Services;

public interface IEncryptionService
{
    (string publicKey, string privateKey) GenerateKeyPair();
    byte[] Encrypt(byte[] data, string publicKey);
    byte[] Decrypt(byte[] encryptedData, string privateKey);
    byte[] Sign(byte[] data, string privateKey);
    bool Verify(byte[] data, byte[] signature, string publicKey);
}
