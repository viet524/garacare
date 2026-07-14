namespace GaraCare.Application.Exceptions;

// Thrown when a referenced entity does not exist. Maps to 404.
public class EntityNotFoundException : BusinessException
{
    public EntityNotFoundException(string message) : base(message)
    {
    }
}
