using System.Security.Cryptography;

namespace GaraCare.Application.Services;

// Sinh mã gồm chữ và số (dùng cho xác minh email lẫn đặt lại mật khẩu) — loại bỏ ký tự
// dễ nhầm (0/O, 1/I) để khách gõ tay từ email không bị lỗi.
public static class VerificationCodeGenerator
{
    private const string Alphabet = "qwertyuiopasdfghjklzxcvbnmABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 6;

    public static string Generate()
    {
        Span<char> code = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(Alphabet.Length);
            code[i] = Alphabet[index];
        }
        return new string(code);
    }
}
