// Plik: MainApi/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;

namespace MainApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Próba wykonania właściwego żądania (np. wejście do kontrolera)
            await _next(context);
        }
        catch (Exception ex)
        {
            // Złapanie dowolnego błędu, który wystąpił w aplikacji
            _logger.LogError(ex, "Wystąpił nieobsłużony wyjątek podczas przetwarzania żądania.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Domyślny kod błędu
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var message = "Wystąpił wewnętrzny błąd serwera.";

        // Możesz tu łatwo mapować konkretne wyjątki na konkretne kody HTTP
        if (exception is KeyNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            message = "Nie znaleziono żądanego zasobu.";
        }
        else if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            message = "Brak dostępu do zasobu.";
        }
        // else if (exception is TwojWlasnyWyjatekBiznesowy) ...

        // Obiekt odpowiedzi
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = message,
            // Wersja deweloperska zwraca szczegóły błędu - na produkcji (Release) można to ukryć
            DetailedError = exception.Message 
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}