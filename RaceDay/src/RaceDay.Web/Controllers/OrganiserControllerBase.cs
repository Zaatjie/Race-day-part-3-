using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base for all Organiser pages: only logged-in Organisers get in.</summary>
[RequireRole(Roles.Organiser)]
public abstract class OrganiserControllerBase : AppController
{
    protected readonly IRaceDayApi Api;
    protected OrganiserControllerBase(IRaceDayApi api) => Api = api;

    /// <summary>The events created by the logged-in organiser (the API lists all; we keep ours).</summary>
    protected async Task<ApiResult<List<EventDto>>> LoadMyEventsAsync()
    {
        var result = await Api.GetEventsAsync();
        if (!result.Success) return result;
        var uid = CurrentUserId;
        return new ApiResult<List<EventDto>>
        {
            Success = true,
            Status = result.Status,
            Data = result.Data!.Where(e => e.OrganiserId == uid).OrderBy(e => e.EventDate).ToList()
        };
    }

    /// <summary>Picks the requested event if it is ours, otherwise the first one.</summary>
    protected static EventDto? Pick(List<EventDto> mine, int? eventId) =>
        mine.FirstOrDefault(e => e.EventId == eventId) ?? mine.FirstOrDefault();
}
