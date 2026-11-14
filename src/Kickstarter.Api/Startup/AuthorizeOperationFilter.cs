using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Kickstarter.Api.Startup;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType;
        var allowsAnonymous =
            method.GetCustomAttributes<AllowAnonymousAttribute>(true).Any()
            || controller?.GetCustomAttributes<AllowAnonymousAttribute>(true).Any() == true;

        if (allowsAnonymous)
        {
            return;
        }

        var requiresAuthorization =
            method.GetCustomAttributes<AuthorizeAttribute>(true).Any()
            || controller?.GetCustomAttributes<AuthorizeAttribute>(true).Any() == true;

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        operation.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] },
        ];
    }
}
