using System.Linq.Expressions;
using DevHabit.Contracts.Tags;
using DevHabit.Contracts.Tags.Requests;

namespace DevHabit.Api.Mappers;

public static class TagMappings
{
    public static Tag ToContract(this Domain.Habits.Entities.Tag tag)
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

    public static Domain.Habits.Entities.Tag ToEntity(this CreateTagRequest tag)
    {
        return new Domain.Habits.Entities.Tag
        {
            Id = $"t_{Guid.CreateVersion7()}",
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static void Update(this Domain.Habits.Entities.Tag tag, UpdateTagRequest updateTagRequest)
    {
        tag.Name = updateTagRequest.Name;
        tag.Description = updateTagRequest.Description;
    }
}

public static class TagQueries
{
    public static Expression<Func<Domain.Habits.Entities.Tag, Tag>> ProjectToContract()
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
