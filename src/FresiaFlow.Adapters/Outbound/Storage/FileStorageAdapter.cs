using FresiaFlow.Application.Ports.Outbound;

namespace FresiaFlow.Adapters.Outbound.Storage;

/// <summary>
/// Adapter para almacenamiento de archivos.
/// Puede implementar almacenamiento local, Azure Blob, S3, etc.
/// </summary>
public class FileStorageAdapter : IFileStorage
{
    private readonly string _basePath;

    public FileStorageAdapter(string basePath)
    {
        _basePath = basePath;
        
        // Asegurar que el directorio base existe
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Generar nombre único
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_basePath, uniqueFileName);

        // Guardar archivo
        using (var fileStreamOut = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOut, cancellationToken);
        }

        // Retornar ruta relativa o absoluta según configuración
        return filePath;
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.IsPathRooted(filePath) 
            ? filePath 
            : Path.Combine(_basePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Archivo no encontrado: {fullPath}");

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_basePath, filePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_basePath, filePath);

        return await Task.FromResult(File.Exists(fullPath));
    }
}

