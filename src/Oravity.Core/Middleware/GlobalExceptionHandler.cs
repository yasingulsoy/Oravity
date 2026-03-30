using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AppException appEx)
        {
            httpContext.Response.StatusCode = appEx.StatusCode;
            var problem = new ProblemDetails
            {
                Status = appEx.StatusCode,
                Title = GetTitle(appEx.StatusCode),
                Detail = appEx.Message
            };
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        // Geçersiz durum geçişi (entity doğrulama) → 400
        if (exception is InvalidOperationException invEx)
        {
            httpContext.Response.StatusCode = 400;
            var problem = new ProblemDetails
            {
                Status = 400,
                Title = "Geçersiz İşlem",
                Detail = invEx.Message
            };
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Beklenmeyen hata: {Message}", exception.Message);
        return false;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Geçersiz İstek",
        401 => "Yetkisiz Erişim",
        403 => "Erişim Engellendi",
        404 => "Bulunamadı",
        409 => "Çakışma",
        422 => "İşlenemeyen Varlık",
        429 => "Çok Fazla İstek",
        _ => "Sunucu Hatası"
    };
}
