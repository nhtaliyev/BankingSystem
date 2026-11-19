using BankingSystem.MVC.Mapping;
using BankingSystem.MVC.Services.ApiClients.Interfaces;
using BankingSystem.MVC.Services.ApiClients.Models;
using BankingSystem.MVC.Services.Http;
using BankingSystem.MVC.ViewModels.Cards;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BankingSystem.MVC.Controllers;

public class CardsController : Controller
{
    private readonly ICardsApi _cardsApi;

    public CardsController(ICardsApi cardsApi)
    {
        _cardsApi = cardsApi;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var card = await _cardsApi.GetByIdAsync(id, cancellationToken);
        return View(card.ToViewModel());
    }

    [HttpGet]
    public IActionResult Create(int accountId) => View(new CreateCardViewModel { AccountId = accountId });

    [HttpPost]
    public async Task<IActionResult> Create(CreateCardViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var card = await _cardsApi.CreateAsync(
                new CreateCardRequest(model.AccountId, model.DailyLimit!.Value), cancellationToken);

            TempData["SuccessMessage"] = "Your new card has been issued.";
            return RedirectToAction(nameof(Details), new { id = card.Id });
        }
        catch (ApiException ex) when (ex.IsBusinessRuleViolation)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var card = await _cardsApi.GetByIdAsync(id, cancellationToken);

        return View(new EditCardViewModel
        {
            Id = card.Id,
            DailyLimit = card.DailyLimit,
            RowVersion = card.RowVersion
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditCardViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _cardsApi.UpdateAsync(
                model.Id, new UpdateCardRequest(model.DailyLimit!.Value, model.RowVersion), cancellationToken);

            TempData["SuccessMessage"] = "The daily limit has been updated.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (ApiException ex) when (ex.IsBusinessRuleViolation)
        {
            ModelState.AddApiErrors(ex);
            return View(model);
        }
    }

    [HttpPost]
    public Task<IActionResult> Freeze(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, () => _cardsApi.FreezeAsync(id, cancellationToken), "The card has been frozen.");

    [HttpPost]
    public Task<IActionResult> Unfreeze(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, () => _cardsApi.UnfreezeAsync(id, cancellationToken), "The card has been unfrozen.");

    [HttpPost]
    public Task<IActionResult> Close(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, () => _cardsApi.CloseAsync(id, cancellationToken), "The card has been closed.");

    [HttpPost]
    public async Task<IActionResult> Reveal(int id, CancellationToken cancellationToken)
    {
        var card = await _cardsApi.GetByIdAsync(id, cancellationToken);

        try
        {
            var revealed = await _cardsApi.RevealAsync(id, cancellationToken);

            var model = card.ToViewModel();
            model.RevealedNumber = FormatCardNumber(revealed.CardNumber);

            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";

            return View(nameof(Details), model);
        }
        catch (ApiException ex) when (ex.IsBusinessRuleViolation || ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // ───────────────────────────── Helpers ─────────────────────────────

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

    private static string FormatCardNumber(string number) =>
        string.Join(' ', Enumerable
            .Range(0, (number.Length + 3) / 4)
            .Select(i => number.Substring(i * 4, Math.Min(4, number.Length - i * 4))));
}