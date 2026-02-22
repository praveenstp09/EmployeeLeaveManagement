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
            var token = context.Request.Headers["token"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token))
            {
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
