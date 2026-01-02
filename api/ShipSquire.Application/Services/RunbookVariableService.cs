using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class RunbookVariableService
{
    private readonly IRunbookRepository _runbookRepository;
    private readonly IRepository<RunbookVariable> _variableRepository;
    private readonly ICurrentUser _currentUser;

    public RunbookVariableService(
        IRunbookRepository runbookRepository,
        IRepository<RunbookVariable> variableRepository,
        ICurrentUser currentUser)
    {
        _runbookRepository = runbookRepository;
        _variableRepository = variableRepository;
        _currentUser = currentUser;
    }

    public async Task<VariableResponse?> CreateAsync(Guid runbookId, VariableRequest request, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(runbookId, _currentUser.UserId, cancellationToken);
        if (runbook == null) return null;

        var variable = new RunbookVariable
        {
            RunbookId = runbookId,
            Name = request.Name,
            ValueHint = request.ValueHint,
            IsSecret = request.IsSecret,
            Description = request.Description
        };

        await _variableRepository.AddAsync(variable, cancellationToken);
        return MapToResponse(variable);
    }

    public async Task<VariableResponse?> UpdateAsync(Guid runbookId, Guid variableId, VariableRequest request, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(runbookId, _currentUser.UserId, cancellationToken);
        if (runbook == null) return null;

        var variable = await _variableRepository.GetByIdAsync(variableId, cancellationToken);
        if (variable == null || variable.RunbookId != runbookId) return null;

        variable.Name = request.Name;
        variable.ValueHint = request.ValueHint;
        variable.IsSecret = request.IsSecret;
        variable.Description = request.Description;
        variable.UpdatedAt = DateTimeOffset.UtcNow;

        await _variableRepository.UpdateAsync(variable, cancellationToken);
        return MapToResponse(variable);
    }

    public async Task<bool> DeleteAsync(Guid runbookId, Guid variableId, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(runbookId, _currentUser.UserId, cancellationToken);
        if (runbook == null) return false;

        var variable = await _variableRepository.GetByIdAsync(variableId, cancellationToken);
        if (variable == null || variable.RunbookId != runbookId) return false;

        await _variableRepository.DeleteAsync(variable, cancellationToken);
        return true;
    }

    private static VariableResponse MapToResponse(RunbookVariable variable)
    {
        return new VariableResponse(
            variable.Id,
            variable.Name,
            variable.ValueHint,
            variable.IsSecret,
            variable.Description
        );
    }
}
