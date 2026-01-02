using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class ServiceService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICurrentUser _currentUser;

    public ServiceService(IServiceRepository serviceRepository, ICurrentUser currentUser)
    {
        _serviceRepository = serviceRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await _serviceRepository.GetByUserIdAsync(_currentUser.UserId, cancellationToken);
        return services.Select(MapToResponse);
    }

    public async Task<ServiceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        return service == null ? null : MapToResponse(service);
    }

    public async Task<ServiceResponse> CreateAsync(ServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = new Service
        {
            UserId = _currentUser.UserId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            RepoProvider = request.Repo?.Provider,
            RepoOwner = request.Repo?.Owner,
            RepoName = request.Repo?.Name,
            RepoUrl = request.Repo?.Url
        };

        await _serviceRepository.AddAsync(service, cancellationToken);
        return MapToResponse(service);
    }

    public async Task<ServiceResponse?> UpdateAsync(Guid id, ServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        if (service == null) return null;

        service.Name = request.Name;
        service.Slug = request.Slug;
        service.Description = request.Description;
        service.RepoProvider = request.Repo?.Provider;
        service.RepoOwner = request.Repo?.Owner;
        service.RepoName = request.Repo?.Name;
        service.RepoUrl = request.Repo?.Url;
        service.UpdatedAt = DateTimeOffset.UtcNow;

        await _serviceRepository.UpdateAsync(service, cancellationToken);
        return MapToResponse(service);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        if (service == null) return false;

        await _serviceRepository.DeleteAsync(service, cancellationToken);
        return true;
    }

    private static ServiceResponse MapToResponse(Service service)
    {
        ServiceRepoInfo? repo = null;
        if (!string.IsNullOrEmpty(service.RepoProvider))
        {
            repo = new ServiceRepoInfo(
                service.RepoProvider,
                service.RepoOwner,
                service.RepoName,
                service.RepoUrl
            );
        }

        return new ServiceResponse(
            service.Id,
            service.Name,
            service.Slug,
            service.Description,
            repo,
            service.CreatedAt,
            service.UpdatedAt
        );
    }
}
