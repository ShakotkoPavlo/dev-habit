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
public class HabitsController(ApplicationDbContext dbContext, LinkService linkService) : ControllerBase
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
            Items = dataShapingService.ShapeCollectionData(habits, habitsRequest.Fields, h => CreateLinksForHabit(h.Id, habitsRequest.Fields)),
            TotalCount = totalCount,
            Page = habitsRequest.Page,
            PageSize = habitsRequest.PageSize,
        };

        paginationResult.Links = CreateLinksForHabit(habitsRequest, paginationResult.HasNextPage, paginationResult.HasPreviousPage);

        return Ok(paginationResult);
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

        shapedHabit.TryAdd("links", CreateLinksForHabit(id, fields));

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

        Habit habitDto = habit.ToContract();
        habitDto.Links = CreateLinksForHabit(habitDto.Id, null);

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

    private List<Link> CreateLinksForHabit(
        SearchHabitsRequest habitsRequest,
        bool hasNextPage,
        bool hasPreviousPage)
    {
        List<Link> links = 
        [
            linkService.GenerateLink(endpoint: nameof(GetHabits), rel: "self", method: HttpMethods.Get, new
            {
                page = habitsRequest.Page,
                pageSize = habitsRequest.PageSize,
                fields = habitsRequest.Fields,
                search = habitsRequest.Search,
                type = habitsRequest.Type,
                status = habitsRequest.Status,
            }),
            linkService.GenerateLink(endpoint: nameof(CreateHabit), rel: "create", method: HttpMethods.Post)
        ];

        if (hasNextPage)
        {
            links.Add(
                linkService.GenerateLink(endpoint: nameof(GetHabits), rel: "next-page", method: HttpMethods.Get, new
                {
                    page = habitsRequest.Page + 1,
                    pageSize = habitsRequest.PageSize,
                    fields = habitsRequest.Fields,
                    search = habitsRequest.Search,
                    type = habitsRequest.Type,
                    status = habitsRequest.Status,
                }));
        }

        if (hasPreviousPage)
        {
            links.Add(
                linkService.GenerateLink(endpoint: nameof(GetHabits), rel: "previous-page", method: HttpMethods.Get, new
                {
                    page = habitsRequest.Page - 1,
                    pageSize = habitsRequest.PageSize,
                    fields = habitsRequest.Fields,
                    search = habitsRequest.Search,
                    type = habitsRequest.Type,
                    status = habitsRequest.Status,
                }));
        }

        return links;
    }

    private List<Link> CreateLinksForHabit(string id, string? fields)
    {
        return
        [
            linkService.GenerateLink(endpoint: nameof(GetHabit), rel: "self", method: HttpMethods.Get, values: new { id, fields }),
            linkService.GenerateLink(endpoint: nameof(UpdateHabit), rel: "update", method: HttpMethods.Put, values: new { id }),
            linkService.GenerateLink(endpoint: nameof(PatchHabit), rel: "partial-update", method: HttpMethods.Patch, values: new { id }),
            linkService.GenerateLink(endpoint: nameof(DeleteHabit), rel: "delete", method: HttpMethods.Delete, values: new { id }),
            linkService.GenerateLink(endpoint: nameof(HabitTagsController.AddTagToHabit), rel: "upsert-tags", method: HttpMethods.Put, values: new { habitId = id }, "HabitTags"),
        ];
    }
}
