namespace ShipSquire.Application.Exceptions;

/// <summary>
/// Base exception for domain-related errors with user-friendly messages.
/// </summary>
public class DomainException : Exception
{
    public string UserMessage { get; }
    public string ErrorCode { get; }

    public DomainException(string errorCode, string userMessage, string? technicalMessage = null)
        : base(technicalMessage ?? userMessage)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage;
    }
}

/// <summary>
/// Exception thrown when a status transition is invalid.
/// </summary>
public class InvalidStatusTransitionException : DomainException
{
    public string CurrentStatus { get; }
    public string RequestedStatus { get; }
    public string[] ValidTransitions { get; }

    public InvalidStatusTransitionException(string currentStatus, string requestedStatus, string[] validTransitions)
        : base(
            "INVALID_STATUS_TRANSITION",
            $"Cannot change status from '{currentStatus}' to '{requestedStatus}'. " +
            (validTransitions.Length > 0
                ? $"Valid next statuses: {string.Join(", ", validTransitions)}."
                : "No transitions available from this status."))
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
        ValidTransitions = validTransitions;
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : DomainException
{
    public string FieldName { get; }

    public ValidationException(string fieldName, string message)
        : base("VALIDATION_ERROR", message)
    {
        FieldName = fieldName;
    }
}

/// <summary>
/// Exception thrown when a resource is not found.
/// </summary>
public class NotFoundException : DomainException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId)
        : base("NOT_FOUND", $"{resourceType} not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
