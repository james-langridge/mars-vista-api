using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarsVista.Api.Filters;

/// <summary>
/// Authorization filter that ensures the user has admin role.
/// Use this attribute on admin controllers/actions to protect them.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Items["UserRole"] as string;

        if (role != "admin")
        {
            context.Result = new JsonResult(new
            {
                error = "Forbidden",
                message = "Admin access required. This endpoint is restricted to administrators."
            })
            {
                StatusCode = 403
            };
        }
    }
}
