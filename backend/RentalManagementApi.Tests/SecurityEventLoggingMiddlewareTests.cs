using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RentalManagementApi.Middleware;

namespace RentalManagementApi.Tests;

public class SecurityEventLoggingMiddlewareTests
{
    [Fact]
    public async Task Middleware_DoesNotThrow_On401Response()
    {
        var logger = new TestLogger<SecurityEventLoggingMiddleware>();
        var mw = new SecurityEventLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        }, logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        // Should not throw
        await mw.InvokeAsync(context);
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_DoesNotThrow_On403Response()
    {
        var logger = new TestLogger<SecurityEventLoggingMiddleware>();
        var mw = new SecurityEventLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        }, logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        await mw.InvokeAsync(context);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_DoesNotLog_On200Response()
    {
        var logger = new TestLogger<SecurityEventLoggingMiddleware>();
        var mw = new SecurityEventLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        }, logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        await mw.InvokeAsync(context);
        Assert.Equal(0, logger.WarningCount);
    }
}

/// <summary>Minimal test logger that counts warnings.</summary>
public class TestLogger<T> : ILogger<T>
{
    public int WarningCount { get; private set; }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning) WarningCount++;
    }
}
