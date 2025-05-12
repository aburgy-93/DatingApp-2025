using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions;
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, 
        IConfiguration config)
    {
        // Allows app to respond to HTTP requests
        services.AddControllers();

        // Adding our connection to the DB to the services and registering the connection string
        services.AddDbContext<DataContext>(opt => 
        {
            opt.UseSqlite(config.GetConnectionString("DefaultConnection"));
        });

        // Registers support for Cross-Origin Resource Sharing so front end and back end can communicate on different ports
        services.AddCors();
        // Every time a class needs the ITokenService, inject a new instance of TokenService for the requested scope
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILikesRepository, LikesRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Adds photo service and action filter (LogUserActivity) to DI
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<LogUserActivity>();

        // Sets up AutoMapper to map between entities and DTO classes
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Adds SignalIR services for real-time communication
        services.AddSignalR();

        // Binds the app settings from appsettings.json to Cloudinary settings 
        services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));

        // Registers PresenceTracker as a singleton -- only one instance is created and shared across the app's lifetime
        services.AddSingleton<PresenceTracker>();

        return services;
    }
}

// Each call like services.AddXyz() registers a service in the dependency injection (DI) container
// Tells ASP.NET Core how and when to create an instance of that service
// Makes the service available throughout the application, so you can request it in the constructors or controllers via DI