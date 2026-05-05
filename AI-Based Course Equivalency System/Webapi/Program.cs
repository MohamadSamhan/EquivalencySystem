using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Service;
using System.Text;

namespace Webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure JWT authentication
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("constring")));

            // Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEquivalencyService, EquivalencyService>();
            builder.Services.AddScoped<ITranscriptService, TranscriptService>();
            builder.Services.AddScoped<IAdminService, AdminService>();

            // HTTP client + OpenAI singleton
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ISimilarityService, OpenAiSimilarityService>();

            // Startup warning if API key missing
            var openAiKey = builder.Configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(openAiKey))
            {
                Console.WriteLine("Warning: OpenAI:ApiKey not configured. Set 'OpenAI__ApiKey' env var or add to appsettings.json.");
            }

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend5500", policy =>
                {
                    policy.WithOrigins("http://localhost:5500")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    // استخدم UserRoleConverter فقط — يقبل رقم ونص عربي وإنجليزي
                    opts.JsonSerializerOptions.Converters.Add(new Domine.Dtos.UserRoleConverter());
                    opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
                app.MapOpenApi();

            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend5500");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
