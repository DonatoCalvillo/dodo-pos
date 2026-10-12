using System.Net.Http.Json;
using System.Text.Json;
using FerreSys.Domain;

namespace FerreSys.Licensing;

public sealed class LicenseApiClient : IDisposable
{
    public const string MissingServerMessage =
        "No hay servidor de licencias configurado. Importa el archivo .lic.";

    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly string _baseUrl;

    public LicenseApiClient(string? baseUrl, HttpClient? httpClient = null)
    {
        _baseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (httpClient == null)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _ownsClient = true;
        }
        else
        {
            _http = httpClient;
            _ownsClient = false;
        }
    }

    public Task<LicenseApiResult> ActivateAsync(
        string email,
        string activationCode,
        string hardwareId,
        CancellationToken cancellationToken = default)
    {
        var body = new ActivateLicenseRequest
        {
            Email = email.Trim(),
            ActivationCode = activationCode.Trim(),
            HardwareId = hardwareId.Trim()
        };
        return PostAsync("v1/licenses/activate", body, cancellationToken);
    }

    public Task<LicenseApiResult> RenewAsync(
        string licenseId,
        string hardwareId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var body = new RenewLicenseRequest
        {
            LicenseId = licenseId.Trim(),
            HardwareId = hardwareId.Trim(),
            Email = email.Trim()
        };
        return PostAsync("v1/licenses/renew", body, cancellationToken);
    }

    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }

    private async Task<LicenseApiResult> PostAsync<T>(string path, T body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            return new LicenseApiResult(false, null, MissingServerMessage);

        try
        {
            using var response = await _http.PostAsJsonAsync($"{_baseUrl}/{path}", body, LicenseJson.Options, cancellationToken)
                .ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LicenseApiError? error = TryReadError(json);
                string message = string.IsNullOrWhiteSpace(error?.Error)
                    ? "No se pudo contactar el servidor de licencias."
                    : error.Error;
                return new LicenseApiResult(false, null, message);
            }

            SignedLicenseContainer? license = JsonSerializer.Deserialize<SignedLicenseContainer>(json, LicenseJson.Options);
            if (license == null || string.IsNullOrWhiteSpace(license.Signature))
                return new LicenseApiResult(false, null, "El servidor devolvió una licencia ilegible.");

            return new LicenseApiResult(true, license, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new LicenseApiResult(false, null, "Sin conexión. La licencia local sigue vigente si aún no vence.");
        }
    }

    private static LicenseApiError? TryReadError(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<LicenseApiError>(json, LicenseJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
