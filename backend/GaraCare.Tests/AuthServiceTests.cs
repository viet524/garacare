using GaraCare.Application.DTOs.Auth;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Infrastructure.Auth;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GaraCare.Tests;

// Fake IDateTimeProvider để kiểm soát "hiện tại" trong test hạn mã xác minh/reset.
public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc);
}

// Fake IEmailService — chỉ ghi lại email cuối cùng đã "gửi" để assert nội dung mã trong test.
public class FakeEmailService : IEmailService
{
    public string? LastToEmail { get; private set; }
    public string? LastSubject { get; private set; }
    public string? LastBody { get; private set; }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        LastToEmail = toEmail;
        LastSubject = subject;
        LastBody = htmlBody;
        return Task.CompletedTask;
    }
}

public class AuthServiceTests
{
    private static (AuthService Service, GaraCareDbContext Db, FakeEmailService Email, FakeDateTimeProvider Clock) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        var email = new FakeEmailService();
        var clock = new FakeDateTimeProvider();
        var jwtSettings = Options.Create(new JwtSettings
        {
            Issuer = "GaraCare.Tests",
            Audience = "GaraCare.Tests.Client",
            Key = "unit-test-signing-key-at-least-32-chars-long",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
        });
        var passwordHasher = new BCryptPasswordHasher();
        var tokenService = new JwtTokenService(jwtSettings, clock);

        var service = new AuthService(unitOfWork, email, clock, passwordHasher, tokenService);
        return (service, db, email, clock);
    }

    private static string ExtractCodeFromEmailBody(string body)
    {
        // Mã nằm trong <p style="...">CODE</p> — lấy nội dung thẻ <p> có style bold.
        var marker = "letter-spacing:4px;\">";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = body.IndexOf("</p>", start, StringComparison.Ordinal);
        return body[start..end];
    }

    [Fact]
    public async Task RegisterCustomerAsync_ValidRequest_CreatesUnverifiedUserAndSendsEmail()
    {
        var (service, db, email, _) = CreateService();
        var request = new RegisterCustomerRequest
        {
            FullName = "Nguyễn Văn An",
            Phone = "0912345678",
            Email = "an@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        };

        var result = await service.RegisterCustomerAsync(request);

        Assert.Contains("kiểm tra email", result.Message, StringComparison.OrdinalIgnoreCase);

        var user = await db.Users.SingleAsync(u => u.Email == "an@example.com");
        Assert.False(user.IsEmailVerified);
        Assert.NotNull(user.EmailVerificationCode);
        Assert.NotNull(user.EmailVerificationCodeExpiresAt);
        Assert.Equal("an@example.com", email.LastToEmail);

        var customer = await db.Customers.SingleAsync(c => c.UserId == user.Id);
        Assert.Equal("Nguyễn Văn An", customer.FullName);
    }

    [Fact]
    public async Task RegisterCustomerAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        var (service, _, _, _) = CreateService();
        var request = new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "dup@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        };

        await service.RegisterCustomerAsync(request);

        await Assert.ThrowsAsync<DuplicateEmailException>(() => service.RegisterCustomerAsync(request));
    }

    [Fact]
    public async Task LoginAsync_UnverifiedAccount_ThrowsForbiddenActionException()
    {
        var (service, _, _, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "unverified@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });

        await Assert.ThrowsAsync<ForbiddenActionException>(() =>
            service.LoginAsync(new LoginRequest { Email = "unverified@example.com", Password = "matkhau123" }));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var (service, _, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "wrongpass@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);
        await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "wrongpass@example.com", Code = code });

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(new LoginRequest { Email = "wrongpass@example.com", Password = "sai-mat-khau" }));
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidCode_ActivatesAccountAndReturnsToken()
    {
        var (service, db, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "verify@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);

        var result = await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "verify@example.com", Code = code });

        Assert.NotEmpty(result.Token);
        Assert.Equal("Customer", result.Role);

        var user = await db.Users.SingleAsync(u => u.Email == "verify@example.com");
        Assert.True(user.IsEmailVerified);
        Assert.Null(user.EmailVerificationCode);

        // Sau khi verify, login phải thành công (không còn bị chặn ForbiddenActionException).
        var loginResult = await service.LoginAsync(new LoginRequest { Email = "verify@example.com", Password = "matkhau123" });
        Assert.NotEmpty(loginResult.Token);
    }

    [Fact]
    public async Task LoginAsync_CustomerRole_TokenContainsCustomerIdClaim()
    {
        var (service, db, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "claimtest@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);
        await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "claimtest@example.com", Code = code });

        var result = await service.LoginAsync(new LoginRequest { Email = "claimtest@example.com", Password = "matkhau123" });

        var customer = await db.Customers.SingleAsync(c => c.Email == "claimtest@example.com");
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var customerIdClaim = jwt.Claims.Single(c => c.Type == "CustomerId").Value;
        Assert.Equal(customer.Id.ToString(), customerIdClaim);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokenPairAndRevokesOld()
    {
        var (service, db, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "refresh@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);
        var loginResult = await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "refresh@example.com", Code = code });

        var refreshed = await service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken });

        Assert.NotEmpty(refreshed.Token);
        Assert.NotEmpty(refreshed.RefreshToken);
        Assert.NotEqual(loginResult.RefreshToken, refreshed.RefreshToken);

        var oldTokenHash = RefreshTokenGenerator.Hash(loginResult.RefreshToken);
        var oldEntity = await db.RefreshTokens.SingleAsync(t => t.TokenHash == oldTokenHash);
        Assert.NotNull(oldEntity.RevokedAt);

        // Refresh token cũ đã bị thu hồi (rotation) — dùng lại phải bị chặn.
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken }));
    }

    [Fact]
    public async Task RefreshTokenAsync_UnknownToken_ThrowsInvalidCredentialsException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "khong-ton-tai" }));
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsInvalidCredentialsException()
    {
        var (service, _, email, clock) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "expiredrefresh@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);
        var loginResult = await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "expiredrefresh@example.com", Code = code });

        clock.UtcNow = clock.UtcNow.AddDays(8); // quá hạn 7 ngày

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken }));
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshToken()
    {
        var (service, db, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "logout@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);
        var loginResult = await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "logout@example.com", Code = code });

        await service.LogoutAsync(new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken });

        var tokenHash = RefreshTokenGenerator.Hash(loginResult.RefreshToken);
        var entity = await db.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash);
        Assert.NotNull(entity.RevokedAt);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken }));
    }

    [Fact]
    public async Task VerifyEmailAsync_WrongCode_ThrowsInvalidVerificationCodeException()
    {
        var (service, _, _, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "wrongcode@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });

        await Assert.ThrowsAsync<InvalidVerificationCodeException>(() =>
            service.VerifyEmailAsync(new VerifyEmailRequest { Email = "wrongcode@example.com", Code = "ZZZZZZ" }));
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredCode_ThrowsInvalidVerificationCodeException()
    {
        var (service, _, email, clock) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "expired@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var code = ExtractCodeFromEmailBody(email.LastBody!);

        clock.UtcNow = clock.UtcNow.AddHours(25); // qua hạn 24h

        await Assert.ThrowsAsync<InvalidVerificationCodeException>(() =>
            service.VerifyEmailAsync(new VerifyEmailRequest { Email = "expired@example.com", Code = code }));
    }

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_ReturnsGenericMessageWithoutRevealing()
    {
        var (service, _, email, _) = CreateService();

        var result = await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "khong-ton-tai@example.com" });

        Assert.Contains("Nếu email tồn tại", result.Message);
        Assert.Null(email.LastToEmail); // không gửi email vì user không tồn tại
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidCode_ChangesPasswordAndAllowsLogin()
    {
        var (service, _, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "reset@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var verifyCode = ExtractCodeFromEmailBody(email.LastBody!);
        await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "reset@example.com", Code = verifyCode });

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "reset@example.com" });
        var resetCode = ExtractCodeFromEmailBody(email.LastBody!);

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "reset@example.com",
            Code = resetCode,
            NewPassword = "matkhaumoi456",
            ConfirmNewPassword = "matkhaumoi456",
        });

        // Mật khẩu cũ không còn dùng được, mật khẩu mới đăng nhập được.
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(new LoginRequest { Email = "reset@example.com", Password = "matkhau123" }));

        var loginResult = await service.LoginAsync(new LoginRequest { Email = "reset@example.com", Password = "matkhaumoi456" });
        Assert.NotEmpty(loginResult.Token);
    }

    [Fact]
    public async Task ResetPasswordAsync_WrongCode_ThrowsInvalidVerificationCodeException()
    {
        var (service, _, email, _) = CreateService();
        await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FullName = "A",
            Phone = "0900000000",
            Email = "wrongreset@example.com",
            Password = "matkhau123",
            ConfirmPassword = "matkhau123",
        });
        var verifyCode = ExtractCodeFromEmailBody(email.LastBody!);
        await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "wrongreset@example.com", Code = verifyCode });
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "wrongreset@example.com" });

        await Assert.ThrowsAsync<InvalidVerificationCodeException>(() =>
            service.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "wrongreset@example.com",
                Code = "WRONG1",
                NewPassword = "matkhaumoi456",
                ConfirmNewPassword = "matkhaumoi456",
            }));
    }
}
