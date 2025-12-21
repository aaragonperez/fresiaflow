namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para almacenamiento de archivos (PDFs, documentos).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Guarda un archivo y retorna su ruta/URL.
    /// </summary>
    Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un archivo como stream.
    /// </summary>
    Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un archivo.
    /// </summary>
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un archivo existe.
    /// </summary>
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
}

