using System.Net;
using Core.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Infrastructure.Common.Validation;

/// <summary>
/// Runs the FluentValidation validator registered for each action argument (e.g. AddBranchModel)
/// before the action. On failure it returns 400 with the first error, in the HRM Response shape.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                context.Result = new BadRequestObjectResult(
                    new Response(result.Errors[0].ErrorMessage, HttpStatusCode.BadRequest, isSuccess: false));
                return;
            }
        }

        await next();
    }
}
