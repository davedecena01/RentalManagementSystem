namespace RentalManagementApi.Middleware;

public class SecurityEventLoggingMiddleware(RequestDelegate next, ILogger<SecurityEventLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode is 401 or 403)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";

            logger.LogWarning(
                "Security event {StatusCode}: {Method} {Path} | user={UserId} ip={IP} ua={UserAgent}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                userId,
                ip,
                userAgent);
        }
    }
}
