using System.Net;
using System.Text.Json;
using GaraCare.Application.Exceptions;

namespace GaraCare.Api.Middleware;

// Maps business exceptions to the HTTP status codes documented in docs/04-api-contract.md.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                InvalidCredentialsException => HttpStatusCode.Unauthorized,
                InvalidTransitionException => HttpStatusCode.BadRequest,
                ForbiddenActionException => HttpStatusCode.Forbidden,
                EntityNotFoundException => HttpStatusCode.NotFound,
                BusinessException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            // Exception nghiệp vụ (400/401/403/404) đã có message soạn sẵn an toàn để hiện cho
            // người dùng (xem các lớp trong GaraCare.Application.Exceptions). Exception KHÔNG rõ
            // nguồn gốc (500) thì ex.Message có thể lộ chi tiết nội bộ (câu SQL, đường dẫn file,
            // tên bảng...) — không được trả nguyên văn ra ngoài, chỉ log server-side rồi trả về
            // một câu chung chung.
            string clientMessage;
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception");
                clientMessage = "Đã có lỗi xảy ra ở hệ thống, vui lòng thử lại sau.";
            }
            else
            {
                clientMessage = ex.Message;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = clientMessage }));
        }
    }
}
