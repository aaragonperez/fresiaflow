namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para integración con OpenAI API específico para Chat AI.
/// Puede usar una API key diferente a la de extracción de facturas.
/// </summary>
public interface IChatAIClient : IOpenAIClient
{
    // Hereda todos los métodos de IOpenAIClient
    // Se puede extender en el futuro con métodos específicos para chat
}
