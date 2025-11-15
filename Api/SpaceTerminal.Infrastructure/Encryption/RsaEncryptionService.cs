using System.Security.Cryptography;
using SpaceTerminal.Core.Services;

namespace SpaceTerminal.Infrastructure.Encryption;

public class RsaEncryptionService : IEncryptionService
{
    private const int KeySize = 4096; // Strong encryption

    public (string publicKey, string privateKey) GenerateKeyPair()
    {
        using var rsa = RSA.Create(KeySize);
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        return (publicKey, privateKey);
    }

    public byte[] Encrypt(byte[] data, string publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] Decrypt(byte[] encryptedData, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] Sign(byte[] data, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public bool Verify(byte[] data, byte[] signature, string publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
