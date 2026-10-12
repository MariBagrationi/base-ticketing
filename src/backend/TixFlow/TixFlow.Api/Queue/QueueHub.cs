using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TixFlow.Api.Auth;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Queue;

[Authorize]
public class QueueHub : Hub
{
    private readonly TixFlowDbContext _db;

    public QueueHub(TixFlowDbContext db)
    {
        _db = db;
    }

    /// <summary>Buyers subscribe to their own admission and to queue-length updates for an event.</summary>
    public async Task JoinQueueGroup(string eventId)
    {
        var userId = Context.UserIdentifier
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (userId is null)
            throw new HubException("Not authenticated.");
        if (Context.User is null || !(Context.User.Has(Role.User) || Context.User.IsAdmin()))
            throw new HubException("Only buyers can join an event queue.");

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(eventId, userId));
        await Groups.AddToGroupAsync(Context.ConnectionId, EventGroup(eventId));
    }

    public async Task LeaveQueueGroup(string eventId)
    {
        var userId = Context.UserIdentifier
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (userId is null) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(eventId, userId));
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, EventGroup(eventId));
    }

    /// <summary>Staff and admins subscribe to live check-in counts for an event they may scan.</summary>
    public async Task JoinCheckinGroup(string eventId)
    {
        if (Context.User is null || !Guid.TryParse(eventId, out var id) || !await _db.CanScanAsync(Context.User, id))
            throw new HubException("You are not allowed to follow check-ins for this event.");

        await Groups.AddToGroupAsync(Context.ConnectionId, CheckinGroup(eventId));
    }

    public Task LeaveCheckinGroup(string eventId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, CheckinGroup(eventId));

    public static string UserGroup(string eventId, string userId) => $"queue:{eventId}:{userId}";
    public static string EventGroup(string eventId) => $"queue:{eventId}";
    public static string CheckinGroup(string eventId) => $"checkin:{eventId}";
}
