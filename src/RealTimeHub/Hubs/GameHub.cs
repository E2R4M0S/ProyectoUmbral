using Microsoft.AspNetCore.SignalR;

namespace RealTimeHub.Hubs;

public class GameHub : Hub
{
    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSessionGroup(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}
