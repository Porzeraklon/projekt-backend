using Microsoft.AspNetCore.SignalR;

namespace WorkerService.Hubs;

// Klasa Huba. Na razie jest pusta, bo Worker będzie tylko "nadawał" do klientów,
// ale musi istnieć, żeby front miał się do czego podłączyć.
public class NotificationHub : Hub
{
}