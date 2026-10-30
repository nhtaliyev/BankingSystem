using BankingSystem.Business.DTOs.CardDTOs;
using BankingSystem.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankingSystem.API.Controllers
{
    [ApiController]
    [Route("api/cards")]
    [Authorize]
    [Produces("application/json")]
    public class CardsController : ControllerBase
    {
        private readonly ICardService _cardService;

        public CardsController(ICardService cardService)
        {
            _cardService = cardService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CardGetDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<CardGetDto>> Create(
            [FromBody] CardCreateDto dto, CancellationToken cancellationToken)
        {
            var card = await _cardService.CreateCardAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CardGetDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CardGetDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var card = await _cardService.GetByIdAsync(id, cancellationToken);
            return Ok(card);
        }

        [HttpGet("account/{accountId:int}")]
        [ProducesResponseType(typeof(ICollection<CardGetDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ICollection<CardGetDto>>> GetByAccountId(
            int accountId, CancellationToken cancellationToken)
        {
            var cards = await _cardService.GetByAccountIdAsync(accountId, cancellationToken);
            return Ok(cards);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(
            int id, [FromBody] CardEditDto dto, CancellationToken cancellationToken)
        {
            await _cardService.UpdateCardAsync(id, dto, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/freeze")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Freeze(int id, CancellationToken cancellationToken)
        {
            await _cardService.FreezeCardAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/unfreeze")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Unfreeze(int id, CancellationToken cancellationToken)
        {
            await _cardService.UnfreezeCardAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/block")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Block(int id, CancellationToken cancellationToken)
        {
            await _cardService.BlockCardAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/unblock")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Unblock(int id, CancellationToken cancellationToken)
        {
            await _cardService.UnblockCardAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/close")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
        {
            await _cardService.CloseCardAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/reveal")]
        [EnableRateLimiting("sensitive")]
        [ProducesResponseType(typeof(CardNumberRevealDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CardNumberRevealDto>> Reveal(int id, CancellationToken cancellationToken)
        {
            var result = await _cardService.RevealCardNumberAsync(id, cancellationToken);

            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";

            return Ok(result);
        }
    }
}