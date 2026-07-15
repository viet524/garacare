namespace GaraCare.Application.Exceptions;

// Số điện thoại đã gắn với một Customer có tài khoản đăng nhập (UserId != null). Maps 400.
public class DuplicatePhoneException : BusinessException
{
    public DuplicatePhoneException(string message) : base(message)
    {
    }
}
