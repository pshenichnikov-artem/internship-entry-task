using Core.Models.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => char.ToLowerInvariant(kvp.Key[0]) + kvp.Key[1..],
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var response = new ApiResponse
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = 400,
                        Message = "Ошибка валидации данных",
                        Details = errors
                    }
                };

                context.Result = new JsonResult(response)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }
    }
}
