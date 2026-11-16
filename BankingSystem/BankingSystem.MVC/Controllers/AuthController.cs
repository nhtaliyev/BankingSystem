using BankingSystem.MVC.Services.ApiClients;
using BankingSystem.MVC.Services.Auth;
using BankingSystem.MVC.Services.Http;
using BankingSystem.MVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace BankingSystem.MVC.Controllers;

public class AuthController : Controller
{
    private readonly IAuthApi _authApi;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthApi authApi, ILogger<AuthController> logger)
    {
        _authApi = authApi;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var tokens = await _authApi.LoginAsync(
                new LoginRequest(model.Email, model.Password, model.RememberMe), cancellationToken);

            await SignInAsync(tokens, model.RememberMe);

            return Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl!)
                : RedirectToAction("Index", "Home");
        }
        catch (ApiException ex)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _authApi.RegisterAsync(
                new RegisterRequest(
                    model.FullName,
                    model.Email,
                    model.Password,
                    model.ConfirmPassword,
                    model.PhoneNumber,
                    model.Birthday!.Value),
                cancellationToken);

            TempData["SuccessMessage"] = "Your account has been created. You can log in now.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiException ex)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            await _authApi.LogoutAsync(cancellationToken);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "API logout failed; signing out locally anyway");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(TokenResponse tokens, bool rememberMe)
    {
        var principal = ClaimsPrincipalFactory.Create(tokens.AccessToken);

        var properties = new AuthenticationProperties { IsPersistent = rememberMe };

        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = tokens.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = tokens.RefreshToken },
            new AuthenticationToken
            {
                Name = "expires_at",
                Value = tokens.ExpireDate.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
            }
        ]);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}