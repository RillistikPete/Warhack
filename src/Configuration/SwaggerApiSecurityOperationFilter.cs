using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using WarHack.Security;

namespace WarHack.Configuration
{
    public sealed class SwaggerApiSecurityOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Puts the padlock only on operations that require authorization
        /// </summary>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            // [AllowAnonymous]
            if (metadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            var authorizeData = metadata.OfType<IAuthorizeData>().ToArray();
            if (authorizeData.Length == 0)
            {
                return;
            }

            var usesApiKey = authorizeData.Any(a =>
                a.AuthenticationSchemes?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Contains(ApiKeyDefaults.Scheme, StringComparer.OrdinalIgnoreCase) == true);

            if (!usesApiKey)
            {
                operation.Description = (operation.Description + "\n\nRequires a signed-in session (Identity cookie).").TrimStart();
                return;
            }

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("apikey", context.Document)] = []
                }
            ];
            // documents status codes when authorization is required, could optionally go as data annotations on controller methods
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid API key." });
            if (authorizeData.Any(a => !string.IsNullOrWhiteSpace(a.Roles)))
            {
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Valid key, but it lacks the required role." });
            }
        }
    }
}
