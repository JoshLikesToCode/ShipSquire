using ShipSquire.Domain.Entities;

namespace ShipSquire.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> GetOrCreateByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByGitHubUserIdAsync(string githubUserId, CancellationToken cancellationToken = default);
}
