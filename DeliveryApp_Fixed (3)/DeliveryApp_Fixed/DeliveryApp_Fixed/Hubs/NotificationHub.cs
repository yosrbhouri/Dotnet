using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DeliveryApp.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // SignalR utilise automatiquement Context.UserIdentifier (= NameIdentifier du cookie Identity)
        // pour router les messages via Clients.User(userId).
    }
}
