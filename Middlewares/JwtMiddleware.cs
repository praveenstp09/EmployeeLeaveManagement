using EmpLeave.Services;

namespace EmpLeave.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TokenService tokenService)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userId = tokenService.ValidateToken(token);
                if (userId != null)
                {
                    context.Items["EmployeeId"] = userId;
                }
            }

            await _next(context);
        }
    }
}
