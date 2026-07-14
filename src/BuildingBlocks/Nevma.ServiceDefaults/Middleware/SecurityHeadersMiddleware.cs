namespace Nevma.ServiceDefaults.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
        headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; frame-ancestors 'none'";

        return next(context);
    }
}
