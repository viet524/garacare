using System.Net;
using GaraCare.Api.Middleware;
using GaraCare.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GaraCare.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [InlineData(typeof(QuotationLockedException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(EmptyQuotationException), HttpStatusCode.BadRequest)]
    public async Task InvokeAsync_BusinessException_MapsToExpectedStatusCode(Type exceptionType, HttpStatusCode expected)
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw (Exception)Activator.CreateInstance(exceptionType, "lỗi")!,
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)expected, context.Response.StatusCode);
    }
}
