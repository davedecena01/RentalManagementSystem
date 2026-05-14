using Microsoft.AspNetCore.Http;
using RentalManagementApi.Middleware;

namespace RentalManagementApi.Tests;

public class SecurityHeadersMiddlewareTests
{
    private static DefaultHttpContext MakeContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task Middleware_SetsXContentTypeOptions()
    {
        var ctx = MakeContext();
        var mw = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);
        Assert.Equal("nosniff", ctx.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsXFrameOptions()
    {
        var ctx = MakeContext();
        var mw = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);
        Assert.Equal("DENY", ctx.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsReferrerPolicy()
    {
        var ctx = MakeContext();
        var mw = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);
        Assert.Equal("strict-origin-when-cross-origin", ctx.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsPermissionsPolicy()
    {
        var ctx = MakeContext();
        var mw = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);
        Assert.Equal("camera=(), microphone=(), geolocation=()", ctx.Response.Headers["Permissions-Policy"].ToString());
    }
}
