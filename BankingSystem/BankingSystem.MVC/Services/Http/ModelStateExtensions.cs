using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BankingSystem.MVC.Services.Http
{
    public static class ModelStateExtensions
    {
        public static void AddApiErrors(this ModelStateDictionary modelState, ApiException exception)
        {
            if (exception.Errors.Count == 0)
            {
                modelState.AddModelError(string.Empty, exception.Message);
                return;
            }

            foreach (var (key, messages) in exception.Errors)
            {
                var field = key.Contains('.') ? key[(key.LastIndexOf('.') + 1)..] : key;

                foreach (var message in messages)
                    modelState.AddModelError(field, message);
            }
        }
    }

}
