using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PricingService.Domain;

namespace PricingService.Infrastructure;

public sealed class PricingExceptionMiddleware(RequestDelegate next, ILogger<PricingExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ValidationException error)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(error.Errors
                .GroupBy(item => item.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(item => item.ErrorMessage).ToArray()))
            { Status = 400, Title = "Invalid pricing request" });
        }
        catch (PricingInputException error) { await Problem(context, 400, error.Message); }
        catch (KeyNotFoundException error) { await Problem(context, 404, error.Message); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            logger.LogError(error, "Pricing request failed");
            await Problem(context, 500, "The pricing request could not be processed.");
        }
    }

    private static Task Problem(HttpContext context, int status, string title)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title });
    }
}
