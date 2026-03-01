using EmpLeave.Config;
using EmpLeave.Services;
using EmpLeave.Services.SupabaseServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmpLeave
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Allow non-UTC DateTime values with PostgreSQL timestamp with time zone columns
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            //builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();

            //builder.Services.AddSwaggerGen();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste ONLY the token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
         {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

            });

            builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
        };
    });

            builder.Services.AddAuthorization();

            builder.Services.AddDbContext<EmployeeLeaveDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null)));

            builder.Services.AddSingleton<TokenService>();

            // Register Supabase Initializer
            builder.Services.AddSingleton<ISupabaseInitializer, SupabaseInitializer>();

            // Initialize Supabase Client
            var supabaseInitializer = builder.Services.BuildServiceProvider().GetRequiredService<ISupabaseInitializer>();
            var supabaseClient = await supabaseInitializer.InitializeAsync();

            // Register File Storage Service with initialized Supabase client
            builder.Services.AddScoped<IFileStorageService>(provider =>
            {
                var bucket = builder.Configuration["Supabase:Bucket"];
                return new SupabaseStorageService(
                    builder.Configuration,
                    supabaseClient,
                    bucket
                );
            });


            // CORS — allow frontend and admin
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseMiddleware<JwtMiddleware>();
            //Using a custom middleware to handle jwt authentication and authorization, which will be applied to all incoming requests. This middleware will check for the presence of a JWT token in the request headers, validate it, and set the user context accordingly. If the token is missing or invalid, the middleware can return an unauthorized response.

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
