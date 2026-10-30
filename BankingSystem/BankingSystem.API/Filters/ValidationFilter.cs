using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankingSystem.API.Filters
{
    public sealed class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var services = context.HttpContext.RequestServices;

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (services.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

                foreach (var failure in result.Errors)
                    context.ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(
                    new ValidationProblemDetails(context.ModelState) { Status = StatusCodes.Status400BadRequest });
                return;
            }

            await next();
        }
    }
}
