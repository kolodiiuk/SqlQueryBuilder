using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SQB.Shared.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger, RequestDelegate next)
    {
        _logger = logger;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid();

            _logger.LogError(ex, $"{errorId} : {ex.Message}");

            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var error = new ErrorInfo
            {
                Id = errorId,
                ErrorMessage = $"Unhandled Error: {ex.Message}   {ex.Source}   {ex.StackTrace}"
            };

            
            await HandldeFileRequestErrors(httpContext, error);
        }
    }

    private static async Task HandldeFileRequestErrors(HttpContext httpContext, ErrorInfo error)
    {
        var requestPath = httpContext.Request.Path.ToString();
        bool isFileRequest = requestPath.Contains("/files", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.EndsWith(".pdf") ||
                             requestPath.EndsWith(".zip") ||
                             requestPath.EndsWith(".csv") ||
                             requestPath.EndsWith(".png") ||
                             requestPath.EndsWith(".jpg");

        if (isFileRequest)
        {
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync("Error: Unable to process file request.");
        }
        else
        {
            var acceptHeader = httpContext.Request.Headers["Accept"].ToString();

            if (acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                httpContext.Response.ContentType = "text/html";
                await httpContext.Response.WriteAsync($"<h1>Error</h1><p>{error.ErrorMessage}</p>");
            }
            else if (acceptHeader.Contains("application/xml", StringComparison.OrdinalIgnoreCase))
            {
                httpContext.Response.ContentType = "application/xml";
                var xml = $"<Error><Id>{error.Id}</Id><ErrorMessage>{error.ErrorMessage}</ErrorMessage></Error>";
                await httpContext.Response.WriteAsync(xml);
            }
            else
            {
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(error);
            }
        }
    }
}