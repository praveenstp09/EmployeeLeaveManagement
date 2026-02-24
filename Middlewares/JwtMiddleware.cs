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
                var tokenInfo = tokenService.ValidateToken(token);
                if (tokenInfo != null)
                {
                    context.Items["EmployeeId"] = tokenInfo.EmployeeId;
                    context.Items["Role"] = tokenInfo.Role;
                }
            }

            await _next(context);
        }
    }
}
