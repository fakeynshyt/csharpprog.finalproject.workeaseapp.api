using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkeaseAPI.Data;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Services;
using WorkEaseAPI.Data;

namespace WorkeaseAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ─────────────────────────────────────────────
            // 1. DATABASE
            // ─────────────────────────────────────────────
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // ─────────────────────────────────────────────
            // 2. SERVICES (DI)
            // ─────────────────────────────────────────────
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IChildService, ChildService>();
            builder.Services.AddScoped<IHealthService, HealthService>();
            builder.Services.AddScoped<IFeeService, FeeService>();
            builder.Services.AddScoped<ICenterService, CenterService>();
            builder.Services.AddScoped<ISyncService, SyncService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IAutoFeeService, AutoFeeService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<IGrowthService, GrowthService>();

            // ─────────────────────────────────────────────
            // 3. AUTHENTICATION (JWT)
            // ─────────────────────────────────────────────
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });

            // ─────────────────────────────────────────────
            // 4. AUTHORIZATION
            // ─────────────────────────────────────────────
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                options.AddPolicy("AdminAndCDW", p => p.RequireRole("Admin", "CDW"));
                options.AddPolicy("ParentOnly", p => p.RequireRole("Parent"));
                options.AddPolicy("AllRoles", p => p.RequireRole("Admin", "CDW", "Parent"));
            });

            // ─────────────────────────────────────────────
            // 5. CORS
            // ─────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            // ─────────────────────────────────────────────
            // 6. CONTROLLERS + SWAGGER
            // ─────────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "WorkEase API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your JWT token"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ─────────────────────────────────────────────
            // BUILD
            // ─────────────────────────────────────────────
            var app = builder.Build();

            // ─────────────────────────────────────────────
            // GLOBAL ERROR HANDLER (FIRST)
            // ─────────────────────────────────────────────
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var error = context.Features
                        .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

                    if (error is not null)
                    {
                        var innerMessage = error.Error.InnerException?.Message
                                           ?? error.Error.Message;

                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = innerMessage,
                            detail = error.Error.Message
                        });
                    }
                });
            });

            // ─────────────────────────────────────────────
            // MIDDLEWARE PIPELINE
            // ─────────────────────────────────────────────
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("AllowAll");

            app.UseAuthentication();   // MUST be before Authorization
            app.UseAuthorization();

            app.MapControllers();

            // ─────────────────────────────────────────────
            // DATABASE MIGRATION + SEED
            // ─────────────────────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.Database.MigrateAsync();
                await DataSeeder.SeedAsync(db);
            }

            // ─────────────────────────────────────────────
            // RUN APP
            // ─────────────────────────────────────────────
            app.Run();
        }
    }
}