using System.Threading;

namespace FresiaFlow.Application.Services;

/// <summary>
/// Servicio para gestionar la cancelación de la generación de asientos contables.
/// </summary>
public class AccountingGenerationCancellationService
{
    private CancellationTokenSource? _currentCancellationTokenSource;
    private readonly object _lock = new object();

    /// <summary>
    /// Crea un nuevo CancellationTokenSource para una operación de generación.
    /// </summary>
    public CancellationToken CreateCancellationToken()
    {
        lock (_lock)
        {
            // Cancelar cualquier operación anterior
            _currentCancellationTokenSource?.Cancel();
            _currentCancellationTokenSource?.Dispose();

            // Crear nuevo token
            _currentCancellationTokenSource = new CancellationTokenSource();
            return _currentCancellationTokenSource.Token;
        }
    }

    /// <summary>
    /// Cancela la operación de generación actual.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _currentCancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// Verifica si hay una operación en progreso.
    /// </summary>
    public bool IsOperationInProgress()
    {
        lock (_lock)
        {
            return _currentCancellationTokenSource != null && 
                   !_currentCancellationTokenSource.Token.IsCancellationRequested;
        }
    }

    /// <summary>
    /// Limpia el token de cancelación actual.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _currentCancellationTokenSource?.Dispose();
            _currentCancellationTokenSource = null;
        }
    }
}

