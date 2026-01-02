using FluentAssertions;
using ShipSquire.Domain.Entities;
using Xunit;

namespace ShipSquire.Tests.Unit.Domain;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void User_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        // Assert
        user.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
        user.Services.Should().NotBeNull();
        user.Runbooks.Should().NotBeNull();
    }
}
