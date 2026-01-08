using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ShipSquireDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> GetOrCreateByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await GetByEmailAsync(email, cancellationToken);
        if (user != null) return user;

        user = new User
        {
            Email = email,
            DisplayName = email.Split('@')[0],
            AuthProvider = "email"
        };

        await AddAsync(user, cancellationToken);
        return user;
    }

    public async Task<User?> GetByGitHubUserIdAsync(string githubUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.GitHubUserId == githubUserId, cancellationToken);
    }
}
