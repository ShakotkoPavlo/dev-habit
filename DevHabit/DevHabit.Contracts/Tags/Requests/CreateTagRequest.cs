using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace DevHabit.Contracts.Tags.Requests;

public sealed record CreateTagRequest
{
    public required string Name { get; set; }

    public string? Description { get; set; }
}
