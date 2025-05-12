using System.Text;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions;

// Set up authentication and uthorization services using Identity and JWT
public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, 
        IConfiguration config)
    {
        // Setting up ASP.NET Core Identity for AppUser class
        services.AddIdentityCore<AppUser>(opt => {
            // Do not require special characters in passwords
            opt.Password.RequireNonAlphanumeric = false;
        })
        // Adding role support, role manageer, and registering the DataContext as the store where users and rolse are persisted in the DB
            .AddRoles<AppRole>()
            .AddRoleManager<RoleManager<AppRole>>()
            .AddEntityFrameworkStores<DataContext>();

        // Setting up JWT auth
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => 
            {
                // Reads in a secret token key from config
                var tokenKey = config["TokenKey"] ?? throw new Exception("Token key not found");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate token by checking the issuer signing key using a symmetric key (shared secret)
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
                    // Does not validate issuer or audience, can enable in production for added security
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                // This allows JWT tokens to be passed via query string for WebSocket connections (SignaIR)
                // Still working on implementation
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context => {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        // Adding policies for role-based access 
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
            .AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
        return services;
    }
}
