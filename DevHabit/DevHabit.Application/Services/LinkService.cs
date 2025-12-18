using DevHabit.Contracts.Habits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DevHabit.Application.Services;

public sealed class LinkService(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
{
    public Link GenerateLink(
        string endpoint,
        string rel,
        string method,
        object? values = null,
        string? controller = null)
    {
        HttpContext httpContext = httpContextAccessor.HttpContext
                                  ?? throw new InvalidOperationException("HTTP context is not available.");

        string? href = linkGenerator.GetUriByAction(
            httpContext,
            endpoint,
            controller,
            values);

        return new Link
        {
            Href = href ?? throw new Exception("Invalid endpoint name."),
            Method = method,
            Rel = rel
        };
    }
}
