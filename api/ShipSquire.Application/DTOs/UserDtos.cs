namespace ShipSquire.Application.DTOs;

public record UserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
