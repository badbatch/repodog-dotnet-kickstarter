using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Kickstarter.Api.Startup;

public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            var info = new OpenApiInfo { Title = "Kickstarter API", Version = description.ApiVersion.ToString() };

            if (description.IsDeprecated)
            {
                info.Description = "This API version has been deprecated.";
            }

            options.SwaggerDoc(description.GroupName, info);
        }
    }
}
