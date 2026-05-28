using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkerService.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User != null && Context.User.IsInRole("Admin"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminsGroup");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User != null && Context.User.IsInRole("Admin"))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminsGroup");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}