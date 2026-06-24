using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Infrastructure.Adapters;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Module.Auth.Infrastructure.Persistence.Repositories;
using EDCL.Shared.Kernel.Ports;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EDCL.Module.Auth;

public static class AuthModuleRegistration
{
    public static IServiceCollection AddEdclModuleAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DbContext (schema: [auth]) ─────────────────────────────────────
        services.AddDbContext<AuthDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(
                configuration.GetConnectionString("EdclDb"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth")
                          .EnableRetryOnFailure(3));
        });

        // ── Repositories ──────────────────────────────────────────────────
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // ── Domain Services ───────────────────────────────────────────────
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // ── Cross-domain Port Adapters ────────────────────────────────────
        services.AddScoped<IDriverPort, DriverPortAdapter>();

        // ── MediatR (scan this module's assembly) ─────────────────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AuthModuleRegistration).Assembly));

        // ── FluentValidation ──────────────────────────────────────────────
        services.AddValidatorsFromAssembly(typeof(AuthModuleRegistration).Assembly);

        // ── JWT Authentication ─────────────────────────────────────────────
        var jwtConfig = configuration.GetSection("Jwt");
        var secretKey = Encoding.UTF8.GetBytes(jwtConfig["AccessTokenSecret"]!);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = jwtConfig["Issuer"],
                    ValidAudience            = jwtConfig["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(secretKey),
                    ClockSkew                = TimeSpan.FromSeconds(30)   // Minimal clock skew
                };

                // Return structured ApiResponse on auth failure (not default challenge)
                opts.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode  = 401;
                        ctx.Response.ContentType = "application/json";
                        var traceId = ctx.HttpContext.Items["TraceId"]?.ToString() ?? string.Empty;
                        await ctx.Response.WriteAsJsonAsync(new
                        {
                            trace_id = traceId,
                            status   = "error",
                            code     = 401,
                            message  = "Token tidak valid atau sudah kadaluarsa.",
                            errors   = (object?)null
                        });
                    },
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode  = 403;
                        ctx.Response.ContentType = "application/json";
                        var traceId = ctx.HttpContext.Items["TraceId"]?.ToString() ?? string.Empty;
                        await ctx.Response.WriteAsJsonAsync(new
                        {
                            trace_id = traceId,
                            status   = "error",
                            code     = 403,
                            message  = "Akses ditolak.",
                            errors   = (object?)null
                        });
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>Apply EF Core migrations for the Auth module on startup.</summary>
    public static async Task ApplyAuthMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync();
    }
}
