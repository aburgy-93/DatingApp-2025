using System;
using System.Net;
using System.Text.Json;
using API.Errors;

namespace API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, 
    IHostEnvironment env)
{
    // This method is called for every HTTP method that hits the middleware
    // HttpContext represents all HTTP-specific information about the request/response
    public async Task InvokeAsync(HttpContext context)
    {   
        try
        {
            await next(context);
        }
        // Setting up the response if an exception occurs
        catch (Exception ex)
        {
            
            logger.LogError(ex, ex.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = env.IsDevelopment()
                ? new ApiExceptions(context.Response.StatusCode, ex.Message, ex.StackTrace)
                : new ApiExceptions(context.Response.StatusCode, ex.Message, "Internal server error");
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}
