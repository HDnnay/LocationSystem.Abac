using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Linq;

namespace Abac.WebApi.Middleware
{
    public class AuthorizationFailureMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthorizationFailureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // 检查是否授权失败
            if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                // 构建详细的错误响应
                var errorResponse = new
                {
                    StatusCode = 403,
                    Message = "授权失败",
                    Details = new[] { 
                        new { 
                            Handler = "AbacAuthorizationHandler", 
                            Message = "访问被拒绝，请检查您的权限设置" 
                        }
                    }
                };

                // 设置响应内容
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                await context.Response.WriteAsync(json);
            }
        }
    }

    public static class AuthorizationFailureMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthorizationFailureHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthorizationFailureMiddleware>();
        }
    }
}