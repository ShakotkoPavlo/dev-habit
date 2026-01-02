using System.Dynamic;
using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Mappers;
using DevHabit.Application.Services;
using DevHabit.Contracts;
using DevHabit.Contracts.Entries;
using DevHabit.Contracts.Habits;
using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entry = DevHabit.Contracts.Entries.Entry;
using Habit = DevHabit.Domain.Entities.Habit;

namespace DevHabit.Api.Controllers;

[Authorize(Roles = Roles.MemberRole)]
[ApiController]
[Route("entries")]
[ApiVersion(1.0)]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypesNames.Application.JsonV1,
    CustomMediaTypesNames.Application.HateoasJson,
    CustomMediaTypesNames.Application.HateoasJsonV1)]
public sealed class EntriesController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEntries(
        [FromQuery] EntriesQueryParameters query,
        DataShapingService dataShapingService)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<Entry>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{query.Fields}'");
        }

        IQueryable<Domain.Entities.Entry> entriesQuery = dbContext.Entries
            .Where(e => e.UserId == userId)
            .Where(e => query.HabitId == null || e.HabitId == query.HabitId)
            .Where(e => query.FromDate == null || e.Date >= query.FromDate)
            .Where(e => query.ToDate == null || e.Date <= query.ToDate)
            .Where(e => query.Source == null || e.Source == Enum.Parse<Domain.Entities.Enums.EntrySource>(query.Source.Value.ToString()))
            .Where(e => query.IsArchived == null || e.IsArchived == query.IsArchived);

        int totalCount = await entriesQuery.CountAsync();

        List<Entry> entries = await entriesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(EntryQueries.ProjectTo())
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                entries,
                query.Fields,
                query.IncludeLinks ? e => CreateLinksForEntry(e.Id, query.Fields, e.IsArchived) : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        if (query.IncludeLinks)
        {
            paginationResult.Links = CreateLinksForEntries(
                query,
                paginationResult.HasNextPage,
                paginationResult.HasPreviousPage);
        }

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEntry(
        string id,
        [FromQuery] EntryQueryParameters query,
        DataShapingService dataShapingService)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<Entry>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{query.Fields}'");
        }

        Entry? entry = await dbContext.Entries
            .Where(e => e.Id == id && e.UserId == userId)
            .Select(EntryQueries.ProjectTo())
            .FirstOrDefaultAsync();

        if (entry is null)
        {
            return NotFound();
        }

        ExpandoObject shapedEntry = dataShapingService.ShapedData(entry, query.Fields);

        if (query.IncludeLinks)
        {
            ((IDictionary<string, object?>)shapedEntry)[nameof(ILinkResponse.Links)] =
                CreateLinksForEntry(id, query.Fields, entry.IsArchived);
        }

        return Ok(shapedEntry);
    }

    [HttpPost]
    public async Task<ActionResult<Entry>> CreateEntry(
        CreateEntryRequest createEntryRequest,
        [FromHeader] AcceptHeader acceptHeader,
        IValidator<CreateEntryRequest> validator)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createEntryRequest);

        Habit? habit = await dbContext.Habits
            .FirstOrDefaultAsync(h => h.Id == createEntryRequest.HabitId && h.UserId == userId);

        if (habit is null)
        {
            return Problem(
                detail: $"Habit with ID '{createEntryRequest.HabitId}' does not exist.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Domain.Entities.Entry entry = createEntryRequest.ToEntity(userId, habit);

        dbContext.Entries.Add(entry);
        await dbContext.SaveChangesAsync();

        Entry entryContract = entry.To();

        if (acceptHeader.IncludeLinks)
        {
            entryContract.Links = CreateLinksForEntry(entryContract.Id, null, entryContract.IsArchived);
        }

        return CreatedAtAction(nameof(GetEntry), new { id = entryContract.Id }, entryContract);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<Entry>>> CreateEntryBatch(
        CreateEntryBatchRequest createEntryBatchRequest,
        [FromHeader] AcceptHeader acceptHeader,
        IValidator<CreateEntryBatchRequest> validator)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createEntryBatchRequest);

        var habitIds = createEntryBatchRequest.Entries
            .Select(e => e.HabitId)
            .ToHashSet();

        List<Habit> existingHabits = await dbContext.Habits
            .Where(h => habitIds.Contains(h.Id) && h.UserId == userId)
            .ToListAsync();

        if (existingHabits.Count != habitIds.Count)
        {
            return Problem(
                detail: "One or more habit IDs is invalid",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var entries = createEntryBatchRequest.Entries
            .Select(dto => dto.ToEntity(userId, existingHabits.First(h => h.Id == dto.HabitId)))
            .ToList();

        dbContext.Entries.AddRange(entries);
        await dbContext.SaveChangesAsync();

        var entrys = entries.Select(e => e.To()).ToList();

        if (acceptHeader.IncludeLinks)
        {
            foreach (Entry entry in entrys)
            {
                entry.Links = CreateLinksForEntry(entry.Id, null, entry.IsArchived);
            }
        }

        return CreatedAtAction(nameof(GetEntries), entrys);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEntry(
        string id,
        UpdateEntry updateEntry,
        IValidator<UpdateEntry> validator)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(updateEntry);

        Domain.Entities.Entry? entry = await dbContext.Entries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.UpdateFrom(updateEntry);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/archive")]
    public async Task<ActionResult> ArchiveEntry(string id)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Domain.Entities.Entry? entry = await dbContext.Entries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = true;
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/un-archive")]
    public async Task<ActionResult> UnArchiveEntry(string id)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Domain.Entities.Entry? entry = await dbContext.Entries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = false;
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEntry(string id)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Domain.Entities.Entry? entry = await dbContext.Entries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        dbContext.Entries.Remove(entry);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<EntryStats>> GetStats()
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entries = await dbContext.Entries
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Date)
            .Select(e => new { e.Date })
            .ToListAsync();

        if (!entries.Any())
        {
            return Ok(new EntryStats
            {
                DailyStats = [],
                TotalEntries = 0,
                CurrentStreak = 0,
                LongestStreak = 0
            });
        }

        // Calculate daily stats
        var dailyStats = entries
            .GroupBy(e => e.Date)
            .Select(g => new DailyStats
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Date)
            .ToList();

        // Calculate total entries
        int totalEntries = entries.Count;

        // Calculate streaks
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dates = entries.Select(e => e.Date).Distinct().OrderBy(d => d).ToList();

        int currentStreak = 0;
        int longestStreak = 0;
        int currentCount = 0;

        // Calculate current streak (must be active up to today)
        for (int i = dates.Count - 1; i >= 0; i--)
        {
            if (i == dates.Count - 1)
            {
                if (dates[i] == today)
                {
                    currentStreak = 1;
                }
                else
                {
                    break;
                }
            }
            else if (dates[i].AddDays(1) == dates[i + 1])
            {
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        // Calculate longest streak
        for (int i = 0; i < dates.Count; i++)
        {
            if (i == 0 || dates[i] == dates[i - 1].AddDays(1))
            {
                currentCount++;
                longestStreak = Math.Max(longestStreak, currentCount);
            }
            else
            {
                currentCount = 1;
            }
        }

        return Ok(new EntryStats
        {
            DailyStats = dailyStats,
            TotalEntries = totalEntries,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak
        });
    }

    private List<Link> CreateLinksForEntries(
        EntriesQueryParameters parameters,
        bool hasNextPage,
        bool hasPreviousPage)
    {
        List<Link> links =
        [
            linkService.GenerateLink(nameof(GetEntries), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                sort = parameters.Sort,
                habitId = parameters.HabitId,
                fromDate = parameters.FromDate,
                toDate = parameters.ToDate,
                source = parameters.Source,
                isArchived = parameters.IsArchived
            }),
            linkService.GenerateLink(nameof(GetStats), "stats", HttpMethods.Get),
            linkService.GenerateLink(nameof(CreateEntry), "create", HttpMethods.Post),
            linkService.GenerateLink(nameof(CreateEntryBatch), "create-batch", HttpMethods.Post)
        ];

        if (hasNextPage)
        {
            links.Add(linkService.GenerateLink(nameof(GetEntries), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                sort = parameters.Sort,
                habitId = parameters.HabitId,
                fromDate = parameters.FromDate,
                toDate = parameters.ToDate,
                source = parameters.Source,
                isArchived = parameters.IsArchived
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.GenerateLink(nameof(GetEntries), "previous-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                sort = parameters.Sort,
                habitId = parameters.HabitId,
                fromDate = parameters.FromDate,
                toDate = parameters.ToDate,
                source = parameters.Source,
                isArchived = parameters.IsArchived
            }));
        }

        return links;
    }

    private List<Link> CreateLinksForEntry(string id, string? fields, bool isArchived)
    {
        List<Link> links =
        [
            linkService.GenerateLink(nameof(GetEntry), "self", HttpMethods.Get, new { id, fields }),
            linkService.GenerateLink(nameof(UpdateEntry), "update", HttpMethods.Put, new { id }),
            isArchived ?
                linkService.GenerateLink(nameof(UnArchiveEntry), "un-archive", HttpMethods.Put, new { id }) :
                linkService.GenerateLink(nameof(ArchiveEntry), "archive", HttpMethods.Put, new { id }),
            linkService.GenerateLink(nameof(DeleteEntry), "delete", HttpMethods.Delete, new { id })
        ];

        return links;
    }
}
