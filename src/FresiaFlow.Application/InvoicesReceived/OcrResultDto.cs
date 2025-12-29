using System.Collections.ObjectModel;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Resultado enriquecido del paso OCR (texto plano + layout básico).
/// </summary>
public sealed record OcrResultDto(
    string Text,
    decimal Confidence,
    IReadOnlyList<OcrPageLayoutDto> Pages)
{
    public static OcrResultDto Empty { get; } =
        new(string.Empty, 0m, Array.Empty<OcrPageLayoutDto>());
}

/// <summary>
/// Layout simple de una página reconocida por OCR.
/// </summary>
public sealed record OcrPageLayoutDto(
    int PageNumber,
    decimal Width,
    decimal Height,
    IReadOnlyList<OcrTextBlockDto> Blocks);

/// <summary>
/// Bloque de texto con bounding box relativo para reconstruir el layout.
/// </summary>
public sealed record OcrTextBlockDto(
    string Text,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    decimal Confidence);

