namespace GaraCare.Application.Exceptions;

// Sai email hoặc mật khẩu lúc đăng nhập. Maps 401.
public class InvalidCredentialsException : BusinessException
{
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
