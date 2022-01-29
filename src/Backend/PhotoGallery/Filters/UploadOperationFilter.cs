using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PhotoGallery.Filters;

public class UploadOperationFilter : IOperationFilter
{
    public const string UploadOperation = nameof(UploadOperation);

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isUploadMethod = context.ApiDescription.ActionDescriptor.EndpointMetadata.Any(p => p.ToString() == UploadOperation);
        if (isUploadMethod)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "multipart/form-data", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Required = new HashSet<string>{ "file" },
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    {
                                        "file", new OpenApiSchema()
                                        {
                                            Type = "string",
                                            Format = "binary"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
