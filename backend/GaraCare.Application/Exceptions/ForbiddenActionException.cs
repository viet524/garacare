namespace GaraCare.Application.Exceptions;

// Thrown when the current actor lacks permission or ownership for the action. Maps to 403.
public class ForbiddenActionException : BusinessException
{
    public ForbiddenActionException(string message) : base(message)
    {
    }
}
