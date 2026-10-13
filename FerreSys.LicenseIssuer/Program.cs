using System.Text.Json;
using FerreSys.Domain;
using FerreSys.Licensing;

namespace FerreSys.LicenseIssuer;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        try
        {
            Dictionary<string, string> options = ParseOptions(args.Skip(1).ToArray());
            switch (args[0].ToLowerInvariant())
            {
                case "keygen":
                    Keygen(options);
                    return 0;
                case "issue":
                    Issue(options);
                    return 0;
                case "code":
                    Code(options);
                    return 0;
                case "revoke":
                    Revoke(options);
                    return 0;
                case "extend":
                    Extend(options);
                    return 0;
                case "serve":
                    Serve(options);
                    return 0;
                default:
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void Keygen(Dictionary<string, string> options)
    {
        Directory.CreateDirectory(IssuerPaths.Root);
        bool force = options.ContainsKey("force");
        string privateXml;
        string publicXml;
        if (File.Exists(IssuerPaths.PrivateKeyPath) && !force)
        {
            privateXml = File.ReadAllText(IssuerPaths.PrivateKeyPath);
            using var rsa = System.Security.Cryptography.RSA.Create();
            rsa.FromXmlString(privateXml);
            publicXml = rsa.ToXmlString(false);
            Console.WriteLine("La llave privada ya existe. No se reemplazó.");
        }
        else
        {
            (privateXml, publicXml) = LicenseSigner.CreateKeyPair();
            File.WriteAllText(IssuerPaths.PrivateKeyPath, privateXml);
            Console.WriteLine("Llave privada creada.");
        }

        Console.WriteLine(IssuerPaths.PrivateKeyPath);
        if (options.TryGetValue("public-out", out string? publicOut) && !string.IsNullOrWhiteSpace(publicOut))
        {
            string? folder = Path.GetDirectoryName(publicOut);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            File.WriteAllText(publicOut, publicXml);
            Console.WriteLine(publicOut);
        }

        Console.WriteLine(publicXml);
    }

    private static void Issue(Dictionary<string, string> options)
    {
        string output = options.TryGetValue("output", out string? outputPath) && !string.IsNullOrWhiteSpace(outputPath)
            ? outputPath
            : Path.Combine(Environment.CurrentDirectory, "license.lic");

        ActivationService service = CreateService();
        (SignedLicenseContainer container, string code) = service.IssueFile(
            Required(options, "email"),
            Required(options, "name"),
            Required(options, "hardware"),
            ReadPlan(options),
            ReadDays(options));

        string? folder = Path.GetDirectoryName(output);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
        File.WriteAllText(output, JsonSerializer.Serialize(container, LicenseJson.Options));

        LicensePayload? payload = JsonSerializer.Deserialize<LicensePayload>(container.JsonPayload, LicenseJson.Options);
        Console.WriteLine(output);
        Console.WriteLine(code);
        Console.WriteLine(payload?.LicenseId);
    }

    private static void Code(Dictionary<string, string> options)
    {
        ActivationRecord record = CreateService().CreateCode(
            Required(options, "email"),
            Required(options, "name"),
            ReadPlan(options),
            ReadDays(options));
        Console.WriteLine(record.Code);
        Console.WriteLine(record.Plan);
        Console.WriteLine(record.ExpiresAt?.ToString("yyyy-MM-dd") ?? "sin vencimiento");
    }

    private static void Extend(Dictionary<string, string> options)
    {
        ActivationService service = CreateService();
        string code = Required(options, "code");
        service.Extend(code, ReadDays(options));
        Console.WriteLine("Vigencia actualizada.");
    }

    private static void Revoke(Dictionary<string, string> options)
    {
        CreateService().Revoke(Required(options, "code"));
        Console.WriteLine("Código revocado.");
    }

    private static void Serve(Dictionary<string, string> options)
    {
        int port = 5080;
        if (options.TryGetValue("port", out string? portText) && !int.TryParse(portText, out port))
            throw new InvalidOperationException("El valor de --port no es un número.");

        ActivationService service = CreateService();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };
        LicenseServer.ListenAsync(service, port, cts.Token).GetAwaiter().GetResult();
    }

    private static ActivationService CreateService()
    {
        if (!File.Exists(IssuerPaths.PrivateKeyPath))
            throw new InvalidOperationException($"No hay llave privada. Ejecuta keygen. Se espera en {IssuerPaths.PrivateKeyPath}");

        string privateXml = File.ReadAllText(IssuerPaths.PrivateKeyPath);
        return new ActivationService(privateXml, new ActivationLedgerStore(IssuerPaths.LedgerPath));
    }

    private static LicensePlan ReadPlan(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("plan", out string? plan) || string.IsNullOrWhiteSpace(plan))
            return LicensePlan.Monthly;
        if (!LicensePlanInfo.TryParse(plan, out LicensePlan parsed))
            throw new InvalidOperationException("El plan debe ser monthly, annual o lifetime.");
        return parsed;
    }

    private static int? ReadDays(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("days", out string? daysText))
            return null;
        if (!int.TryParse(daysText, out int days))
            throw new InvalidOperationException("El valor de --days no es un número.");
        return days;
    }

    private static string Required(Dictionary<string, string> options, string name)
    {
        if (!options.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Falta --{name}.");
        return value;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                continue;

            string key = args[i][2..];
            string value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            options[key] = value;
        }

        return options;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            FerreSys.LicenseIssuer
              keygen [--force] [--public-out ruta.xml]
              code --email correo --name "Nombre" [--plan monthly|annual|lifetime] [--days N]
              extend --code FS-XXXX-XXXX [--days N]
              revoke --code FS-XXXX-XXXX
              issue --email correo --name "Nombre" --hardware ID [--plan monthly|annual|lifetime] [--days N] [--output license.lic]
              serve [--port 5080]
            La llave privada queda en %USERPROFILE%\.ferresys\licensing\private.xml
            """);
    }
}
