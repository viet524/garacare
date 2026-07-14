namespace GaraCare.Application.Exceptions;

// Email đã được dùng để đăng ký. Maps 400.
public class DuplicateEmailException : BusinessException
{
    public DuplicateEmailException(string message) : base(message)
    {
    }
}
