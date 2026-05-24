using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BusinessLayer.Services
{
    public class CsrfHeaderOperationFilter : IOperationFilter
    {
        private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST",
            "PUT",
            "PATCH",
            "DELETE"
        };
        
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var httpMethod = context.ApiDescription.HttpMethod;

            if (string.IsNullOrWhiteSpace(httpMethod) || !UnsafeMethods.Contains(httpMethod))
                return;

            operation.Parameters ??= new List<IOpenApiParameter>();

            var hasCsrfHeader = operation.Parameters.Any(p =>
                string.Equals(p.Name, "X-CSRF-TOKEN", StringComparison.OrdinalIgnoreCase) &&
                p.In == ParameterLocation.Header);

            if (hasCsrfHeader)
                return;
        }
    }
}