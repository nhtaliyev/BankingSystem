using BankingSystem.MVC.Services.Http;
using BankingSystem.MVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics;

namespace BankingSystem.MVC.Filters;

public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly IModelMetadataProvider _metadataProvider;
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(
        ITempDataDictionaryFactory tempDataFactory,
        IModelMetadataProvider metadataProvider,
        ILogger<ApiExceptionFilter> logger)
    {
        _tempDataFactory = tempDataFactory;
        _metadataProvider = metadataProvider;
        _logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiException exception)
            return;

        var http = context.HttpContext;
        context.ExceptionHandled = true;

        if (exception.IsUnauthorized)
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _tempDataFactory.GetTempData(http)["ErrorMessage"] = "Your session has expired. Please log in again.";

            var returnUrl = HttpMethods.IsGet(http.Request.Method)
                ? http.Request.Path + http.Request.QueryString
                : null;

            context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
        }
        else if (exception.IsForbidden)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
        }
        else if (exception.IsNotFound)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status404NotFound);
        }
        else
        {
            if ((int)exception.StatusCode >= 500)
                _logger.LogWarning(exception, "API call failed with status {StatusCode}", (int)exception.StatusCode);

            var status = (int)exception.StatusCode is >= 400 and <= 599 ? (int)exception.StatusCode : 500;

            context.Result = new ViewResult
            {
                ViewName = "ApiError",
                StatusCode = status,
                ViewData = new ViewDataDictionary<ErrorViewModel>(_metadataProvider, context.ModelState)
                {
                    Model = new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? http.TraceIdentifier,
                        Message = exception.Message
                    }
                }
            };
        }
    }
}