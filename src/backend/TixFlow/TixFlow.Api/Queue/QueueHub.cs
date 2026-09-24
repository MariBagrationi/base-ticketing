using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TixFlow.Api.Queue;

[Authorize]
public class QueueHub : Hub
{
    public async Task JoinQueueGroup(string eventId)
    {
        var userId = Context.UserIdentifier
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (userId is null)
            throw new HubException("Not authenticated.");

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

    public static string UserGroup(string eventId, string userId) => $"queue:{eventId}:{userId}";
    public static string EventGroup(string eventId) => $"queue:{eventId}";
}
