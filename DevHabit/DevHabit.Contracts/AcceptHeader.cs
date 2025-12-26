using Microsoft.AspNetCore.Mvc;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace DevHabit.Contracts;

public record AcceptHeader
{
    [FromHeader(Name = "Accept")]
    public string? Accept { get; set; }

    public bool IncludeLinks =>
        MediaTypeHeaderValue.TryParse(Accept, out MediaTypeHeaderValue? mediaTypeHeaderValue) &&
        mediaTypeHeaderValue.SubTypeWithoutSuffix.HasValue &&
        mediaTypeHeaderValue.SubTypeWithoutSuffix.Value.Contains(CustomMediaTypesNames.Application.HateoasSubType);
}
