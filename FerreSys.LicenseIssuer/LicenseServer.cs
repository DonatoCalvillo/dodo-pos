using System.Net;
using System.Text;
using System.Text.Json;
using FerreSys.Domain;
using FerreSys.Licensing;

namespace FerreSys.LicenseIssuer;

internal static class LicenseServer
{
    public static async Task ListenAsync(ActivationService service, int port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Console.WriteLine($"Escuchando http://localhost:{port}");
        Console.WriteLine("POST /v1/licenses/activate");
        Console.WriteLine("POST /v1/licenses/renew");

        using CancellationTokenRegistration stop = cancellationToken.Register(() => listener.Stop());
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested || !listener.IsListening)
            {
                break;
            }

            _ = Task.Run(() => Handle(service, context), CancellationToken.None);
        }
    }

    private static void Handle(ActivationService service, HttpListenerContext context)
    {
        try
        {
            HttpListenerRequest request = context.Request;
            string path = request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                WriteError(context.Response, 405, "Método no permitido.");
                return;
            }

            if (request.ContentLength64 > 65_536)
            {
                WriteError(context.Response, 400, "La petición es demasiado grande.");
                return;
            }

            string body = ReadBody(request);
            if (string.Equals(path, "/v1/licenses/activate", StringComparison.OrdinalIgnoreCase))
            {
                ActivateLicenseRequest? input = JsonSerializer.Deserialize<ActivateLicenseRequest>(body, LicenseJson.Options);
                if (input == null)
                {
                    WriteError(context.Response, 400, "Formato inválido.");
                    return;
                }

                SignedLicenseContainer license = service.Activate(input.Email, input.ActivationCode, input.HardwareId);
                WriteLicense(context.Response, license);
                return;
            }

            if (string.Equals(path, "/v1/licenses/renew", StringComparison.OrdinalIgnoreCase))
            {
                RenewLicenseRequest? input = JsonSerializer.Deserialize<RenewLicenseRequest>(body, LicenseJson.Options);
                if (input == null)
                {
                    WriteError(context.Response, 400, "Formato inválido.");
                    return;
                }

                SignedLicenseContainer license = service.Renew(input.LicenseId, input.HardwareId, input.Email);
                WriteLicense(context.Response, license);
                return;
            }

            WriteError(context.Response, 404, "Ruta no encontrada.");
        }
        catch (InvalidOperationException ex)
        {
            WriteError(context.Response, 400, ex.Message);
        }
        catch (JsonException)
        {
            WriteError(context.Response, 400, "Formato inválido.");
        }
        catch (Exception ex)
        {
            WriteError(context.Response, 500, ex.Message);
        }
    }

    private static string ReadBody(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void WriteLicense(HttpListenerResponse response, SignedLicenseContainer license)
    {
        WriteJson(response, 200, JsonSerializer.Serialize(license, LicenseJson.Options));
    }

    private static void WriteError(HttpListenerResponse response, int status, string error)
    {
        string json = JsonSerializer.Serialize(new LicenseApiError { Error = error }, LicenseJson.Options);
        WriteJson(response, status, json);
    }

    private static void WriteJson(HttpListenerResponse response, int status, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        response.StatusCode = status;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = body.Length;
        response.OutputStream.Write(body);
        response.Close();
    }
}
