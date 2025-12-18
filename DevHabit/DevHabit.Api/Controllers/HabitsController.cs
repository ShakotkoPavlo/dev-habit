using System.Dynamic;
using DevHabit.Api.Mappers;
using DevHabit.Application.Services;
using DevHabit.Contracts.Habits;
using DevHabit.Contracts.Habits.Requests;
using DevHabit.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits")]
public class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHabits([FromQuery] SearchHabitsRequest habitsRequest, DataShapingService dataShapingService)
    {
        if (!dataShapingService.Validate<Habit>(habitsRequest.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields isn't valid: '{habitsRequest.Fields}'");
        }

        habitsRequest.Search ??= habitsRequest.Search?.Trim().ToLowerInvariant();

        IQueryable<Habit> habitsQuery = dbContext
            .Habits
            .Where(h => habitsRequest.Search == null ||
                        h.Name.Contains(habitsRequest.Search, StringComparison.InvariantCultureIgnoreCase) ||
                        h.Description != null && h.Description.Contains(habitsRequest.Search, StringComparison.InvariantCultureIgnoreCase))
            .Where(h => habitsRequest.Type == null || h.Type.ToString() == habitsRequest.Type.Value.ToString())
            .Where(h => habitsRequest.Status == null || h.Status.ToString() == habitsRequest.Status.ToString())
            .Skip((habitsRequest.Page - 1) * habitsRequest.PageSize)
            .Take(habitsRequest.PageSize)
            .Select(HabitQueries.ProjectToContract());

        int totalCount = await habitsQuery.CountAsync();

        List<Habit> habits = await habitsQuery
            .Skip((habitsRequest.Page - 1) * habitsRequest.PageSize)
            .Take(habitsRequest.PageSize)
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(habits, habitsRequest.Fields),
            TotalCount = totalCount,
            Page = habitsRequest.Page,
            PageSize = habitsRequest.PageSize
        };

        return Ok(paginationResult.Items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHabit(
        string id,
        string? fields,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken = default)
    {
        if (!dataShapingService.Validate<Habit>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields isn't valid: '{fields}'");
        }

        Habit? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToContract())
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
        {
            return NotFound(id);
        }

        ExpandoObject shapedHabit = dataShapingService.ShapedData(habit, fields);

        return Ok(shapedHabit);
    }

    [HttpPost]
    public async Task<ActionResult<Habit>> CreateHabit(
        CreateHabitRequest habitRequest,
        IValidator<CreateHabitRequest> validator,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(habitRequest, cancellationToken);

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
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<Habit> patchDocument, CancellationToken cancellationToken = default)
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
