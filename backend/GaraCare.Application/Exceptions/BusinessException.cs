namespace GaraCare.Application.Exceptions;

// Base type for predictable business-rule failures (invalid transition, expired token, ...).
// Middleware in GaraCare.Api maps subclasses to the HTTP status codes in docs/04-api-contract.md.
public abstract class BusinessException : Exception
{
    protected BusinessException(string message) : base(message)
    {
    }
}
