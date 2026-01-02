using DevHabit.Contracts.Tags.Requests;
using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[Authorize(Roles = $"{Roles.MemberRole}")]
[ApiController]
[Route("habits/{habitId}/tags")]
public class HabitTagsController(ApplicationDbContext dbContext): ControllerBase
{
    [HttpPut]
    public async Task<ActionResult> AddTagToHabit(string habitId, UpsertTagsRequest upsertTagsRequest, CancellationToken cancellationToken = default)
    {
        Habit? habit = await dbContext
            .Habits
            .Include(h => h.HabitTags)
            .FirstOrDefaultAsync(h => h.Id == habitId, cancellationToken);

        if (habit is null)
        {
            return NotFound();
        }

        var currentTags = habit.HabitTags.Select(x => x.TagId).ToHashSet();

        if (currentTags.SetEquals(upsertTagsRequest.Tags))
        {
            return NoContent();
        }

        List<string> existingTagIds = await dbContext.Tags
            .Where(x => upsertTagsRequest.Tags.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingTagIds.Count != upsertTagsRequest.Tags.Count())
        {
            return BadRequest("One or more Ids are invalid!");
        }

        habit.HabitTags.RemoveAll(x => !upsertTagsRequest.Tags.Contains(x.TagId));

        string[] tagsToAdd = upsertTagsRequest.Tags.Except(currentTags).ToArray();

        habit.HabitTags.AddRange(tagsToAdd.Select(id => new HabitTag
        {
            HabitId = habit.Id,
            TagId = id,
            CreatedAtUtc = DateTime.UtcNow,
        }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> RemoveTagFromHabit(string habitId, string id, CancellationToken cancellationToken = default)
    {
        HabitTag? habit = await dbContext
            .HabitTags
            .SingleOrDefaultAsync(h => h.HabitId == habitId && h.TagId == id, cancellationToken);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.HabitTags.Remove(habit);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
