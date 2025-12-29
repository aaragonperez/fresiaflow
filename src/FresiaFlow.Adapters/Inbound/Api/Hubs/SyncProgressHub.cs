using FresiaFlow.Application.Ports.Outbound;
using Microsoft.AspNetCore.SignalR;

namespace FresiaFlow.Adapters.Inbound.Api.Hubs;

public class SyncProgressHub : Hub
{
    public async Task SendProgress(SyncProgressUpdate update)
    {
        await Clients.All.SendAsync("ReceiveProgress", update);
    }
    
    public async Task SendAccountingProgress(AccountingProgressUpdate update)
    {
        await Clients.All.SendAsync("ReceiveAccountingProgress", update);
    }
}

