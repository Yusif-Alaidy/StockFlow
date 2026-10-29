using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
namespace stockFlow.OpenApi;
public sealed class ContractTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var route = context.Description.RelativePath ?? "";
        if (!route.StartsWith("api/v1/", StringComparison.Ordinal)) return;
        var method = context.Description.HttpMethod ?? "GET";
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var anonymous = metadata.OfType<IAllowAnonymous>().Any();
        var roles = metadata.OfType<IAuthorizeData>().Where(x => !string.IsNullOrEmpty(x.Roles)).Select(x => x.Roles).LastOrDefault();
        var permissions = anonymous ? "Public; authentication rate limit applies." : roles is null ? "Authenticated; warehouse access is enforced." : $"Roles: {roles}. Warehouse access is enforced for documents and warehouse resources.";
        var requiresMatch = method == "PUT" || (method == "POST" && context.Description.ParameterDescriptions.Any(x => x.Name == "id"));
        operation.Description = permissions + " See docs/api-contract.md for validation, examples, errors and retries."
            + (requiresMatch ? " Requires one strong If-Match ETag. A retry of an already-posted document returns its existing result even with a stale valid ETag." : "");
        operation.Summary ??= method + " " + route;
        if (!anonymous)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] });
        }
        operation.Responses ??= new OpenApiResponses();
        var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, ct);
        foreach (var status in new[] { "400", "401", "403", "404", "409", "412", "428", "429", "500", "503" })
            operation.Responses[status] = new OpenApiResponse {
                Description = status switch { "400" => "Invalid JSON, DTO, field limits or decimal scale", "401" => "Missing/expired/revoked token", "403" => "Role or warehouse access denied", "404" => "Missing or inaccessible resource", "409" => "Duplicate, state, stock or reference conflict", "412" => "Stale ETag", "428" => "If-Match required", "429" => "Authentication rate limit", "503" => "Database busy; reload before retry", _ => "Safe internal error" },
                Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = new() { Schema = errorSchema } } };
        if (requiresMatch)
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter { Name = "If-Match", In = ParameterLocation.Header, Required = true,
                Description = "The exact quoted ETag from the last detail/create response; no weak tags, wildcard or multiple tags.", Schema = new OpenApiSchema { Type = JsonSchemaType.String } });
        }
        if (operation.Responses.TryGetValue("200", out var response) && response is OpenApiResponse success && !route.StartsWith("api/v1/reports") && !route.EndsWith("login") && !route.EndsWith("refresh"))
        {
            success.Headers ??= new Dictionary<string, IOpenApiHeader>();
            success.Headers["ETag"] = new OpenApiHeader { Description = "Present on single-resource responses. Quoted SQL rowversion; lists carry raw base64 version per item.", Schema = new OpenApiSchema { Type = JsonSchemaType.String } };
        }
    }
}
