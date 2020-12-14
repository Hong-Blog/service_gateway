using Microsoft.AspNetCore.Builder;

namespace service_gateway.Middleware
{
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseHongAuthenticationMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}