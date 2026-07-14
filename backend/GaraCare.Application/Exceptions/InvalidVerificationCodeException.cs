namespace GaraCare.Application.Exceptions;

// Mã xác minh/đặt lại mật khẩu sai hoặc đã hết hạn. Maps 400.
public class InvalidVerificationCodeException : BusinessException
{
    public InvalidVerificationCodeException(string message) : base(message)
    {
    }
}
