using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WarHack.Identity;
namespace WarHack;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RestrictAccessAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = (WarHackUser?)context.HttpContext.Items["User"];
        if (user == null)
        {
            context.Result = new JsonResult( new {message = "Unauthorized"}) {StatusCode = StatusCodes.Status401Unauthorized};
        }
    }
}