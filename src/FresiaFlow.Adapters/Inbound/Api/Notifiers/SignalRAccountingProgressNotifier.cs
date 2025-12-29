using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Adapters.Inbound.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FresiaFlow.Adapters.Inbound.Api.Notifiers;

/// <summary>
/// Notificador de progreso para generación de asientos contables usando SignalR.
/// </summary>
public class SignalRAccountingProgressNotifier : IAccountingProgressNotifier
{
    private readonly IHubContext<SyncProgressHub> _hubContext;

    public SignalRAccountingProgressNotifier(IHubContext<SyncProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(AccountingProgressUpdate update, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAccountingProgress", update, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error pero no lanzar excepción para no interrumpir la generación
            System.Diagnostics.Debug.WriteLine($"Error enviando progreso de contabilidad: {ex.Message}");
        }
    }
}

