namespace FerreSys.Infrastructure.UI;

internal static class UpdateSettings
{
    /// <summary>
    /// Repositorio de GitHub del que Velopack baja los releases publicados desde main.
    /// </summary>
    public const string FeedUrl = "https://github.com/DonatoCalvillo/dodo-pos";

    /// <summary>
    /// API de licencias, sin barra final. Vacío hasta que exista el servidor.
    /// El emisor local imprime una URL como http://localhost:5080
    /// </summary>
    public const string LicenseApiBaseUrl = "";
}
