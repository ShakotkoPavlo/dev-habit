using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DevHabit.Api;

public sealed class ConfigureSwaggerUIOptions(IApiVersionDescriptionProvider versionDescriptionProvider) : IConfigureNamedOptions<SwaggerUIOptions>
{
    public void Configure(SwaggerUIOptions options)
    {
        foreach (ApiVersionDescription versionDescription in versionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{versionDescription.GroupName}/swagger.json", versionDescription.GroupName);
        }
    }

    public void Configure(string? name, SwaggerUIOptions options)
    {
        Configure(options);
    }
}
