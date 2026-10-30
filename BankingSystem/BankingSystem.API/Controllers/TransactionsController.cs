using BankingSystem.Business.DTOs.TransactionDTOs;
using BankingSystem.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.API.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    [Produces("application/json")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICurrentUserService _currentUser;

        public TransactionsController(
            ITransactionService transactionService,
            ICurrentUserService currentUser)
        {
            _transactionService = transactionService;
            _currentUser = currentUser;
        }

        private string CallerId => _currentUser.UserId;

        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransactionGetDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<TransactionGetDto>> Transfer(
            [FromBody] TransactionCreateDto dto, CancellationToken cancellationToken)
        {
            var transaction = await _transactionService.TransferAsync(CallerId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TransactionGetDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<TransactionGetDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var transaction = await _transactionService.GetByIdAsync(CallerId, id, cancellationToken);
            return Ok(transaction);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<TransactionGetDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TransactionGetDto>>> GetMine(CancellationToken cancellationToken)
        {
            var list = await _transactionService.GetMyTransactionsAsync(CallerId, cancellationToken);
            return Ok(list);
        }
    }
}