using BankingSystem.MVC.Mapping;
using BankingSystem.MVC.Services.ApiClients.Interfaces;
using BankingSystem.MVC.Services.ApiClients.Models;
using BankingSystem.MVC.Services.Http;
using BankingSystem.MVC.ViewModels.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.MVC.Controllers;

public class AccountsController : Controller
{
    private readonly IAccountsApi _accountsApi;
    private readonly ICardsApi _cardsApi;

    public AccountsController(IAccountsApi accountsApi, ICardsApi cardsApi)
    {
        _accountsApi = accountsApi;
        _cardsApi = cardsApi;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await _accountsApi.GetMineAsync(cancellationToken);
        return View(accounts.Select(ToViewModel).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var account = await _accountsApi.GetByIdAsync(id, cancellationToken);

        var cards = await _cardsApi.GetByAccountAsync(id, cancellationToken);

        var model = ToViewModel(account);
        model.Cards = cards.Select(card => card.ToViewModel()).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateAccountViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var account = await _accountsApi.CreateAsync(new CreateAccountRequest(model.Currency!.Value), cancellationToken);

            TempData["SuccessMessage"] = "Your new account has been opened.";
            return RedirectToAction(nameof(Details), new { id = account.Id });
        }
        catch (ApiException ex) when (ex.IsBusinessRuleViolation)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }
    }

    [HttpPost]
    public Task<IActionResult> Freeze(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, () => _accountsApi.FreezeAsync(id, cancellationToken), "The account has been frozen.");

    [HttpPost]
    public Task<IActionResult> Unfreeze(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, () => _accountsApi.UnfreezeAsync(id, cancellationToken), "The account has been unfrozen.");

    [HttpPost]
    public Task<IActionResult> Close(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, () => _accountsApi.CloseAsync(id, cancellationToken), "The account has been closed.");

    private async Task<IActionResult> ChangeStatusAsync(int id, Func<Task> apiCall, string successMessage)
    {
        try
        {
            await apiCall();
            TempData["SuccessMessage"] = successMessage;
        }
        catch (ApiException ex) when (ex.IsBusinessRuleViolation)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static AccountViewModel ToViewModel(AccountResponse account) => new()
    {
        Id = account.Id,
        AccountNumber = account.AccountNumber,
        Balance = account.Balance,
        Currency = account.Currency,
        Status = account.Status
    };
}