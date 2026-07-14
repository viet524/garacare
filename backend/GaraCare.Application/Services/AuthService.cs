using GaraCare.Application.DTOs.Auth;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.Services;

public class AuthService : IAuthService
{
    // Thời hạn mã xác minh tài khoản dài hơn mã đặt lại mật khẩu vì mã reset nhạy cảm hơn
    // (cho phép chiếm quyền tài khoản nếu lộ) — con số cụ thể là giả định kỹ thuật, chưa
    // có yêu cầu chính xác từ người dùng.
    private static readonly TimeSpan EmailVerificationExpiry = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetExpiry = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<MessageResponse> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var users = _unitOfWork.Repository<User>();

        var existing = await users.FindAsync(u => u.Email == request.Email, cancellationToken);
        if (existing.Count > 0)
        {
            throw new DuplicateEmailException("Email này đã được đăng ký.");
        }

        var now = _dateTimeProvider.UtcNow;
        var code = VerificationCodeGenerator.Generate();

        var user = new User
        {
            // Đăng nhập bằng Email — Username vẫn là field bắt buộc/duy nhất trong schema
            // (dùng cho tài khoản nội bộ tạo qua Admin), nên với Customer tự đăng ký ta
            // gán Username = Email để không lộ thêm 1 field "tên đăng nhập" ra form khách.
            Username = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Role = UserRole.Customer,
            IsEmailVerified = false,
            EmailVerificationCode = code,
            EmailVerificationCodeExpiresAt = now.Add(EmailVerificationExpiry),
        };

        await users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var customer = new Customer
        {
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            UserId = user.Id,
        };
        await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendAsync(
            request.Email,
            "Xác minh tài khoản GaraCare",
            BuildVerificationEmailBody(request.FullName, code),
            cancellationToken);

        return new MessageResponse
        {
            Message = "Đăng ký thành công. Vui lòng kiểm tra email để lấy mã xác minh tài khoản."
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException("Sai email hoặc mật khẩu.");
        }

        if (!user.IsEmailVerified)
        {
            throw new ForbiddenActionException("Tài khoản chưa xác minh email. Vui lòng kiểm tra email hoặc yêu cầu gửi lại mã.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidVerificationCodeException("Mã xác minh không hợp lệ.");

        if (!user.IsEmailVerified)
        {
            var now = _dateTimeProvider.UtcNow;
            var codeMatches = string.Equals(user.EmailVerificationCode, request.Code, StringComparison.OrdinalIgnoreCase);
            var notExpired = user.EmailVerificationCodeExpiresAt.HasValue && user.EmailVerificationCodeExpiresAt.Value >= now;

            if (!codeMatches || !notExpired)
            {
                throw new InvalidVerificationCodeException("Mã xác minh không đúng hoặc đã hết hạn.");
            }

            user.IsEmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiresAt = null;
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return BuildAuthResponse(user);
    }

    public async Task<MessageResponse> ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default)
    {
        const string genericMessage = "Nếu email tồn tại và chưa xác minh, một mã mới đã được gửi.";

        var user = await FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.IsEmailVerified)
        {
            // Không tiết lộ email có tồn tại hay không / đã xác minh hay chưa.
            return new MessageResponse { Message = genericMessage };
        }

        var code = VerificationCodeGenerator.Generate();
        user.EmailVerificationCode = code;
        user.EmailVerificationCodeExpiresAt = _dateTimeProvider.UtcNow.Add(EmailVerificationExpiry);
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendAsync(
            user.Email!,
            "Xác minh tài khoản GaraCare",
            BuildVerificationEmailBody(user.FullName, code),
            cancellationToken);

        return new MessageResponse { Message = genericMessage };
    }

    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        const string genericMessage = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi mã đặt lại mật khẩu.";

        var user = await FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            // Không tiết lộ email có tồn tại hay không (tránh dò tài khoản).
            return new MessageResponse { Message = genericMessage };
        }

        var code = VerificationCodeGenerator.Generate();
        user.PasswordResetCode = code;
        user.PasswordResetCodeExpiresAt = _dateTimeProvider.UtcNow.Add(PasswordResetExpiry);
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendAsync(
            user.Email!,
            "Đặt lại mật khẩu GaraCare",
            BuildPasswordResetEmailBody(user.FullName, code),
            cancellationToken);

        return new MessageResponse { Message = genericMessage };
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidVerificationCodeException("Mã đặt lại mật khẩu không hợp lệ.");

        var now = _dateTimeProvider.UtcNow;
        var codeMatches = string.Equals(user.PasswordResetCode, request.Code, StringComparison.OrdinalIgnoreCase);
        var notExpired = user.PasswordResetCodeExpiresAt.HasValue && user.PasswordResetCodeExpiresAt.Value >= now;

        if (!codeMatches || !notExpired)
        {
            throw new InvalidVerificationCodeException("Mã đặt lại mật khẩu không đúng hoặc đã hết hạn.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiresAt = null;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." };
    }

    private async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var matches = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == email, cancellationToken);
        return matches.FirstOrDefault();
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        return new AuthResponse
        {
            Token = _tokenService.GenerateToken(user),
            Role = user.Role.ToString(),
            UserId = user.Id,
            FullName = user.FullName,
        };
    }

    private static string BuildVerificationEmailBody(string fullName, string code)
    {
        return $"""
            <p>Chào {fullName},</p>
            <p>Mã xác minh tài khoản GaraCare của bạn là:</p>
            <p style="font-size:24px;font-weight:bold;letter-spacing:4px;">{code}</p>
            <p>Nhập mã này trong ứng dụng để kích hoạt tài khoản. Mã có hiệu lực trong 24 giờ.</p>
            """;
    }

    private static string BuildPasswordResetEmailBody(string fullName, string code)
    {
        return $"""
            <p>Chào {fullName},</p>
            <p>Mã đặt lại mật khẩu GaraCare của bạn là:</p>
            <p style="font-size:24px;font-weight:bold;letter-spacing:4px;">{code}</p>
            <p>Nhập mã này trong ứng dụng để đặt mật khẩu mới. Mã có hiệu lực trong 1 giờ.
            Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
            """;
    }
}
