using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Adapters.Inbound.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FresiaFlow.Adapters.Inbound.Api.Notifiers;

/// <summary>
/// Adaptador inbound para enviar progreso de sincronización vía SignalR.
/// </summary>
public class SignalRSyncProgressNotifier : ISyncProgressNotifier
{
    private readonly IHubContext<SyncProgressHub> _hubContext;

    public SignalRSyncProgressNotifier(IHubContext<SyncProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAsync(SyncProgressUpdate update, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync("ReceiveProgress", update, cancellationToken);
    }
}

