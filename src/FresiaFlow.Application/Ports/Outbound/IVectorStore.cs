namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para almacenamiento vectorial (RAG).
/// Permite almacenar y buscar procedimientos internos usando embeddings.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Almacena un documento con su embedding.
    /// </summary>
    Task StoreDocumentAsync(
        string documentId,
        string content,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca documentos similares usando búsqueda semántica.
    /// </summary>
    Task<List<SearchResult>> SearchSimilarAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un documento del almacén.
    /// </summary>
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de una búsqueda en el vector store.
/// </summary>
public record SearchResult(
    string DocumentId,
    string Content,
    double SimilarityScore,
    Dictionary<string, string> Metadata
);

