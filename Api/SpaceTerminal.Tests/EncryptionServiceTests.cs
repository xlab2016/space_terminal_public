using System.Text;
using SpaceTerminal.Infrastructure.Encryption;
using Xunit;

namespace SpaceTerminal.Tests;

public class EncryptionServiceTests
{
    private readonly RsaEncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _encryptionService = new RsaEncryptionService();
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnValidKeys()
    {
        // Act
        var (publicKey, privateKey) = _encryptionService.GenerateKeyPair();

        // Assert
        Assert.NotNull(publicKey);
        Assert.NotNull(privateKey);
        Assert.NotEmpty(publicKey);
        Assert.NotEmpty(privateKey);
    }

    [Fact]
    public void Encrypt_Decrypt_ShouldWorkCorrectly()
    {
        // Arrange
        var (publicKey, privateKey) = _encryptionService.GenerateKeyPair();
        var originalData = Encoding.UTF8.GetBytes("Test message");

        // Act
        var encryptedData = _encryptionService.Encrypt(originalData, publicKey);
        var decryptedData = _encryptionService.Decrypt(encryptedData, privateKey);

        // Assert
        Assert.Equal(originalData, decryptedData);
    }

    [Fact]
    public void Sign_Verify_ShouldWorkCorrectly()
    {
        // Arrange
        var (publicKey, privateKey) = _encryptionService.GenerateKeyPair();
        var data = Encoding.UTF8.GetBytes("Test message");

        // Act
        var signature = _encryptionService.Sign(data, privateKey);
        var isValid = _encryptionService.Verify(data, signature, publicKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Verify_WithWrongData_ShouldReturnFalse()
    {
        // Arrange
        var (publicKey, privateKey) = _encryptionService.GenerateKeyPair();
        var data = Encoding.UTF8.GetBytes("Test message");
        var wrongData = Encoding.UTF8.GetBytes("Wrong message");

        // Act
        var signature = _encryptionService.Sign(data, privateKey);
        var isValid = _encryptionService.Verify(wrongData, signature, publicKey);

        // Assert
        Assert.False(isValid);
    }
}
