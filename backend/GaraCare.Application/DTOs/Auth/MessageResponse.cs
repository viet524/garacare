namespace GaraCare.Application.DTOs.Auth;

// Response chung cho các bước không trả token (đăng ký, quên mật khẩu, gửi lại mã...).
public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}
