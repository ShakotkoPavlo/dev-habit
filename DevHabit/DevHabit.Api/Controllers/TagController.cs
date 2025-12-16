using DevHabit.Api.Mappers;
using DevHabit.Contracts.Tags.Requests;
using DevHabit.Domain.Habits.Entities;
using DevHabit.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Tag = DevHabit.Contracts.Tags.Tag;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("tags")]
public class TagController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IList<Tag>>> GetTags(CancellationToken cancellationToken = default)
    {
        List<Tag> tags = await dbContext
            .Tags
            .Select(TagQueries.ProjectToContract())
            .ToListAsync(cancellationToken);

        return Ok(tags);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Tag>> GetTag(string id, CancellationToken cancellationToken = default)
    {
        Tag? tag = await dbContext
            .Tags
            .Where(t => t.Id == id)
            .Select(TagQueries.ProjectToContract())
            .FirstOrDefaultAsync(cancellationToken);

        if (tag is null)
        {
            return NotFound(id);
        }

        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<Tag>> CreateTag(
        CreateTagRequest request,
        IValidator<CreateTagRequest> validator,
        ProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        Domain.Habits.Entities.Tag newTag = request.ToEntity();

        if (await dbContext.Tags.AnyAsync(t => t.Name == request.Name, cancellationToken))
        {
            return Problem(
                detail: $"Tag '{request.Name}' already exist.",
                statusCode: StatusCodes.Status409Conflict);
        }

        await dbContext.Tags.AddAsync(newTag, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        Tag tag = newTag.ToContract();

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
    }

    [HttpPut]
    public async Task<ActionResult<Tag>> UpdateTag(string id, UpdateTagRequest request)
    {
        Domain.Habits.Entities.Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound(id);
        }

        tag.Update(request);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteTag(string id)
    {
        Domain.Habits.Entities.Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound(id);
        }

        dbContext.Tags.Remove(tag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
