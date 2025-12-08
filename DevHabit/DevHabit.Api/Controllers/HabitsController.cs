using DevHabit.Api.Mappers;
using DevHabit.Contracts.Habits;
using DevHabit.Contracts.Habits.Requests;
using DevHabit.Infrastructure.Database;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits")]
public class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IList<Habit>>> GetHabits()
    {
        List<Habit> contractHabits = await dbContext.Habits
            .Select(HabitQueries.ProjectToContract())
            .ToListAsync();

        return Ok(contractHabits);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Habit>> GetHabit(string id, CancellationToken cancellationToken = default)
    {
        Habit? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToContract())
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
        {
            return NotFound(id);
        }

        return Ok(habit);
    }

    [HttpPost]
    public async Task<ActionResult<Habit>> CreateHabit(CreateHabitRequest habitRequest, CancellationToken cancellationToken = default)
    {
        Domain.Habits.Entities.Habit habit = habitRequest.ToEntity();

        await dbContext.Habits.AddAsync(habit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetHabit), new { id = habit.Id}, habit);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, UpdateHabitRequest request, CancellationToken cancellationToken = default)
    {
        Domain.Habits.Entities.Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromContract(request);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<Habit> patchDocument,
        CancellationToken cancellationToken = default)
    {
        Domain.Habits.Entities.Habit? domainHabit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (domainHabit is null)
        {
            return NotFound();
        }

        Habit habit = domainHabit.ToContract();

        patchDocument.ApplyTo(habit, ModelState);

        if (!TryValidateModel(habit))
        {
            return ValidationProblem(ModelState);
        }

        domainHabit.Name = habit.Name;
        domainHabit.Description = habit.Description;
        domainHabit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id, CancellationToken cancellationToken = default)
    {
        Domain.Habits.Entities.Habit? domainHabit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (domainHabit is null)
        {
            return NotFound();
        }

        dbContext.Habits.Remove(domainHabit);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
