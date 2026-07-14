namespace GaraCare.Application.Exceptions;

// Thrown when a status transition is requested from an invalid source state. Maps to 400.
public class InvalidTransitionException : BusinessException
{
    public InvalidTransitionException(string message) : base(message)
    {
    }
}
