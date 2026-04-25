
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
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

            builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ────────────────────────────────────────────────────────────────
            // 2. SERVICES — all Scoped (fresh instance per HTTP request)
            // ────────────────────────────────────────────────────────────────
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IChildService, ChildService>();
            builder.Services.AddScoped<IHealthService, HealthService>();
            builder.Services.AddScoped<IFeeService, FeeService>();
            builder.Services.AddScoped<ISyncService, SyncService>();

            // ────────────────────────────────────────────────────────────────
            // 3. JWT AUTHENTICATION
            // ────────────────────────────────────────────────────────────────
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
                                                       Encoding.UTF8.GetBytes(
                                                           builder.Configuration["Jwt:Key"]!))
                    };
                });

            // ────────────────────────────────────────────────────────────────
            // 4. AUTHORIZATION POLICIES
            // ────────────────────────────────────────────────────────────────
            builder.Services.AddAuthorization(options =>
            {
                // Only the Windows Forms admin
                options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

                // Admin + CDW workers (MAUI)
                options.AddPolicy("AdminAndCDW", p => p.RequireRole("Admin", "CDW"));

                // Parents only (MAUI)
                options.AddPolicy("ParentOnly", p => p.RequireRole("Parent"));

                // Everyone who is logged in
                options.AddPolicy("AllRoles", p => p.RequireRole("Admin", "CDW", "Parent"));
            });

            // ────────────────────────────────────────────────────────────────
            // 5. CORS — allow MAUI and Windows Forms to connect
            // ────────────────────────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            // ────────────────────────────────────────────────────────────────
            // 6. CONTROLLERS + SWAGGER
            // ────────────────────────────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            // ✅ New — adds Authorize button to Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "WorkEase API",
                    Version = "v1"
                });

                // Tell Swagger to use JWT Bearer tokens
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your JWT token here. Example: eyJhbGci..."
                });

                // Make every endpoint require the token by default
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ────────────────────────────────────────────────────────────────
            // BUILD & MIDDLEWARE PIPELINE
            // Order matters here — do not rearrange
            // ────────────────────────────────────────────────────────────────
            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("AllowAll");
            app.UseAuthentication();   // must come before Authorization
            app.UseAuthorization();

            app.MapControllers();

            using(var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.Database.MigrateAsync();

                await DataSeeder.SeedAsync(db);
            }

            app.Run();

        }
    }
}
