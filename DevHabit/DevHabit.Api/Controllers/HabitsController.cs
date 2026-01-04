using System.Dynamic;
using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Mappers;
using DevHabit.Application.Services;
using DevHabit.Contracts;
using DevHabit.Contracts.Habits;
using DevHabit.Contracts.Habits.Requests;
using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Habit = DevHabit.Contracts.Habits.Habit;

namespace DevHabit.Api.Controllers;

[EnableRateLimiting("default")]
//[ResponseCache(Duration = 120)]
[Authorize(Roles = $"{Roles.MemberRole}")]
[ApiController]
[Route("habits")]
[ApiVersion(1.0)]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypesNames.Application.JsonV1,
    CustomMediaTypesNames.Application.JsonV2,
    CustomMediaTypesNames.Application.HateoasJson,
    CustomMediaTypesNames.Application.HateoasJsonV1,
    CustomMediaTypesNames.Application.HateoasJsonV2)]
public class HabitsController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHabits([FromQuery] SearchHabitsRequest habitsRequest, DataShapingService dataShapingService, CancellationToken token = default)
    {
        string? userId = await userContext.GetUserIdAsync(token);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<Habit>(habitsRequest.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields isn't valid: '{habitsRequest.Fields}'");
        }

        habitsRequest.Search ??= habitsRequest.Search?.Trim().ToLowerInvariant();

        IQueryable<HabitWithTags> habitsQuery = dbContext
            .Habits
            .Where(h => h.UserId == userId)
            .Where(h => habitsRequest.Search == null ||
                        h.Name.Contains(habitsRequest.Search, StringComparison.InvariantCultureIgnoreCase) ||
                        h.Description != null && h.Description.Contains(habitsRequest.Search, StringComparison.InvariantCultureIgnoreCase))
            .Where(h => habitsRequest.Type == null || h.Type.ToString() == habitsRequest.Type.Value.ToString())
            .Where(h => habitsRequest.Status == null || h.Status.ToString() == habitsRequest.Status.ToString())
            .Skip((habitsRequest.Page - 1) * habitsRequest.PageSize)
            .Take(habitsRequest.PageSize)
            .Select(HabitQueries.ProjectToContract());

        int totalCount = await habitsQuery.CountAsync(token);

        List<HabitWithTags> habits = await habitsQuery
            .Skip((habitsRequest.Page - 1) * habitsRequest.PageSize)
            .Take(habitsRequest.PageSize)
            .ToListAsync(token);

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(habits, habitsRequest.Fields, habitsRequest.IncludeLinks ? h => CreateLinksForHabit(h.Id, habitsRequest.Fields) : null),
            TotalCount = totalCount,
            Page = habitsRequest.Page,
            PageSize = habitsRequest.PageSize,
        };

        if (habitsRequest.IncludeLinks)
        {
            paginationResult.Links = CreateLinksForHabit(habitsRequest, paginationResult.HasNextPage, paginationResult.HasPreviousPage);
        }

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    [MapToApiVersion(1.0)]
    public async Task<IActionResult> GetHabit(
        string id,
        [FromQuery] SearchHabitsRequest habitsRequest,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<Habit>(habitsRequest.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields isn't valid: '{habitsRequest.Fields}'");
        }

        HabitWithTags? habit = await dbContext
            .Habits
            .Where(h => h.UserId == userId)
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToContract())
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
        {
            return NotFound(id);
        }

        ExpandoObject shapedHabit = dataShapingService.ShapedData(habit, habitsRequest.Fields);

        if (habitsRequest.IncludeLinks)
        {
            shapedHabit.TryAdd("links", CreateLinksForHabit(id, habitsRequest.Fields));
        }

        return Ok(shapedHabit);
    }


    [HttpGet("{id}")]
    [ApiVersion(2.0)]
    public async Task<IActionResult> GetHabitV2(
        string id,
        [FromQuery] SearchHabitsRequest habitsRequest,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<Habit>(habitsRequest.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields isn't valid: '{habitsRequest.Fields}'");
        }

        HabitWithTagsV2? habit = await dbContext
            .Habits
            .Where(h => h.Id == id && h.UserId == userId)
            .Select(HabitQueries.ProjectToContractV2())
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
        {
            return NotFound(id);
        }

        ExpandoObject shapedHabit = dataShapingService.ShapedData(habit, habitsRequest.Fields);

        if (habitsRequest.IncludeLinks)
        {
            shapedHabit.TryAdd("links", CreateLinksForHabit(id, habitsRequest.Fields));
        }

        return Ok(shapedHabit);
    }

    [HttpPost]
    public async Task<ActionResult<Habit>> CreateHabit(
        CreateHabitRequest habitRequest,
        IValidator<CreateHabitRequest> validator,
        CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(habitRequest, cancellationToken);

        Domain.Entities.Habit habit = habitRequest.ToEntity(userId);

        await dbContext.Habits.AddAsync(habit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Habit habitContract = habit.ToContract();
        habitContract.Links = CreateLinksForHabit(habitContract.Id, null);

        return CreatedAtAction(nameof(GetHabit), new { id = habitContract.Id}, habitContract);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, UpdateHabitRequest request, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

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
        Domain.Entities.Habit? domainHabit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

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
        Domain.Entities.Habit? domainHabit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

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
