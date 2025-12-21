namespace FresiaFlow.Infrastructure;

/// <summary>
/// Clase de configuración para valores de la aplicación.
/// </summary>
public static class Configuration
{
    public const string DefaultConnectionStringName = "DefaultConnection";
    
    public static class OpenAI
    {
        public const string ApiKeyName = "OpenAI:ApiKey";
        public const string ModelName = "OpenAI:Model";
        public const string DefaultModel = "gpt-4";
    }

    public static class Banking
    {
        public const string ProviderName = "Banking:Provider";
        public const string ApiKeyName = "Banking:ApiKey";
        public const string BaseUrlName = "Banking:BaseUrl";
    }

    public static class Storage
    {
        public const string BasePathName = "Storage:BasePath";
    }
}

