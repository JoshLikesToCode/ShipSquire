using FluentAssertions;
using ShipSquire.Application.Services;
using Xunit;

namespace ShipSquire.Tests.Unit.Services;

public class TokenEncryptionServiceTests
{
    [Fact]
    public void Encrypt_ShouldReturnEncryptedString()
    {
        // Arrange
        var service = new TokenEncryptionService("test-encryption-key-minimum-32chars");
        var plainText = "my-secret-token-12345";

        // Act
        var encrypted = service.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalString()
    {
        // Arrange
        var service = new TokenEncryptionService("test-encryption-key-minimum-32chars");
        var plainText = "my-secret-token-12345";
        var encrypted = service.Encrypt(plainText);

        // Act
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithNullOrEmpty_ShouldReturnSameValue()
    {
        // Arrange
        var service = new TokenEncryptionService("test-encryption-key-minimum-32chars");

        // Act & Assert
        service.Encrypt(null!).Should().BeNull();
        service.Encrypt(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithNullOrEmpty_ShouldReturnSameValue()
    {
        // Arrange
        var service = new TokenEncryptionService("test-encryption-key-minimum-32chars");

        // Act & Assert
        service.Decrypt(null!).Should().BeNull();
        service.Decrypt(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var service = new TokenEncryptionService("test-encryption-key-minimum-32chars");
        var originalToken = "gho_1234567890abcdefghijklmnopqrstuvwxyz";

        // Act
        var encrypted = service.Encrypt(originalToken);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalToken);
    }
}
