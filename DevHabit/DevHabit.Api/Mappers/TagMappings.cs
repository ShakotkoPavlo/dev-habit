using System.Linq.Expressions;
using DevHabit.Contracts.Tags.Requests;
using Tag = DevHabit.Contracts.Tags.Tag;

namespace DevHabit.Api.Mappers;

public static class TagMappings
{
    public static Tag ToContract(this Domain.Entities.Tag tag)
    {
        return new Tag
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = tag.CreatedAtUtc,
            UpdatedAtUtc = tag.UpdatedAtUtc
        };
    }

    public static Domain.Entities.Tag ToEntity(this CreateTagRequest tag, string userId)
    {
        return new Domain.Entities.Tag
        {
            Id = $"t_{Guid.CreateVersion7()}",
            UserId = userId,
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static void Update(this Domain.Entities.Tag tag, UpdateTagRequest updateTagRequest)
    {
        tag.Name = updateTagRequest.Name;
        tag.Description = updateTagRequest.Description;
    }
}

public static class TagQueries
{
    public static Expression<Func<Domain.Entities.Tag, Tag>> ProjectToContract()
    {
        return tag => new Tag
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = tag.CreatedAtUtc,
            UpdatedAtUtc = tag.UpdatedAtUtc
        };
    }
}
