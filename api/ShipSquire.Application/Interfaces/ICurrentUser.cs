namespace ShipSquire.Application.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
}
