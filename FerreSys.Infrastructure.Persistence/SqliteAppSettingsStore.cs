using System.Globalization;
using System.Text.Json;
using FerreSys.Domain;
using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public sealed class SqliteAppSettingsStore : IAppSettingsStore
{
    private readonly SqliteDbContext _dbContext;

    public SqliteAppSettingsStore(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public BusinessTicketProfile GetTicketProfile()
    {
        var values = ReadAll();
        return new BusinessTicketProfile
        {
            BusinessName = Get(values, "ticket.businessName", string.Empty),
            Address = Get(values, "ticket.address", "Calle Principal #123"),
            Rfc = Get(values, "ticket.rfc", string.Empty),
            Phone = Get(values, "ticket.phone", string.Empty),
            ThankYouMessage = Get(values, "ticket.thankYou", "¡Gracias por su compra!"),
            ReturnPolicy = Get(values, "ticket.returnPolicy", "Conserve su ticket para cualquier aclaración."),
            Email = Get(values, "ticket.email", string.Empty),
            Website = Get(values, "ticket.website", string.Empty),
            FooterNote = Get(values, "ticket.footerNote", string.Empty),
            LogoFile = Get(values, "ticket.logoFile", string.Empty),
            PaperWidth = ParsePaper(Get(values, "ticket.paperWidth", nameof(TicketPaperWidth.Mm58))),
            HeaderLayout = values.ContainsKey("ticket.headerLayout")
                ? TicketLayout.Parse(values["ticket.headerLayout"], TicketLayout.HeaderCatalog)
                : TicketLayout.DefaultHeader,
            FooterLayout = values.ContainsKey("ticket.footerLayout")
                ? TicketLayout.Parse(values["ticket.footerLayout"], TicketLayout.FooterCatalog)
                : TicketLayout.DefaultFooter
        };
    }

    public string? ResolveTicketLogoPath()
    {
        string path = LogoPath();
        return File.Exists(path) ? path : null;
    }

    public void SaveTicketLogo(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("No se encontró la imagen del logo.", sourcePath);

        Directory.CreateDirectory(_dbContext.DataFolder);
        string destination = LogoPath();
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            return;

        File.Copy(sourcePath, destination, overwrite: true);
    }

    public void RemoveTicketLogo()
    {
        string path = LogoPath();
        if (File.Exists(path))
            File.Delete(path);
    }

    private string LogoPath() =>
        Path.Combine(_dbContext.DataFolder, BusinessTicketProfile.LogoFileName);

    public void SaveTicketProfile(BusinessTicketProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Write(new Dictionary<string, string>
        {
            ["ticket.businessName"] = profile.BusinessName.Trim(),
            ["ticket.address"] = profile.Address.Trim(),
            ["ticket.rfc"] = profile.Rfc.Trim(),
            ["ticket.phone"] = profile.Phone.Trim(),
            ["ticket.thankYou"] = profile.ThankYouMessage.Trim(),
            ["ticket.returnPolicy"] = profile.ReturnPolicy.Trim(),
            ["ticket.email"] = profile.Email.Trim(),
            ["ticket.website"] = profile.Website.Trim(),
            ["ticket.footerNote"] = profile.FooterNote.Trim(),
            ["ticket.logoFile"] = profile.LogoFile.Trim(),
            ["ticket.paperWidth"] = profile.PaperWidth.ToString(),
            ["ticket.headerLayout"] = TicketLayout.Format(profile.HeaderLayout),
            ["ticket.footerLayout"] = TicketLayout.Format(profile.FooterLayout)
        });
    }

    public PeripheralSettings GetPeripheralSettings()
    {
        var values = ReadAll();
        return new PeripheralSettings
        {
            PrinterTransport = ParseEnum(Get(values, "printer.transport", nameof(PrinterTransport.None)), PrinterTransport.None),
            WindowsPrinterName = Get(values, "printer.windowsName", string.Empty),
            PrinterSerialPort = Get(values, "printer.serialPort", string.Empty),
            PrinterBaudRate = ParseInt(Get(values, "printer.baudRate", "9600"), 9600),
            NetworkHost = Get(values, "printer.host", string.Empty),
            NetworkPort = ParseInt(Get(values, "printer.port", "9100"), 9100),
            OpenDrawerOnCash = Get(values, "printer.openDrawer", "1") == "1",
            DrawerPin = ParseInt(Get(values, "printer.drawerPin", "0"), 0) == 1 ? 1 : 0,
            ScannerMode = ParseEnum(Get(values, "scanner.mode", nameof(ScannerMode.Keyboard)), ScannerMode.Keyboard),
            ScannerSerialPort = Get(values, "scanner.serialPort", string.Empty),
            ScannerBaudRate = ParseInt(Get(values, "scanner.baudRate", "9600"), 9600),
            ScannerAddToCart = Get(values, "scanner.addToCart", "1") != "0",
            ScaleEnabled = Get(values, "scale.enabled", "0") == "1",
            ScaleSerialPort = Get(values, "scale.serialPort", string.Empty),
            ScaleBaudRate = ParseInt(Get(values, "scale.baudRate", "9600"), 9600)
        };
    }

    public bool IsSeedOfferCompleted()
    {
        var values = ReadAll();
        return Get(values, "SeedOfferCompleted", "0") == "1";
    }

    public void MarkSeedOfferCompleted()
    {
        Write(new Dictionary<string, string>
        {
            ["SeedOfferCompleted"] = "1"
        });
    }

    public string? GetLicenseKey()
    {
        var values = ReadAll();
        string license = Get(values, "license.key", string.Empty).Trim();
        return license.Length == 0 ? null : license;
    }

    public void SaveLicenseKey(string licenseKey)
    {
        Write(new Dictionary<string, string>
        {
            ["license.key"] = licenseKey.Trim()
        });
    }

    public LocalBackupSettings GetLocalBackupSettings()
    {
        var values = ReadAll();
        int hours = LocalBackupSettings.Normalize(ParseInt(Get(values, "backup.intervalHours", "0"), 0));
        string raw = Get(values, "backup.lastUtc", string.Empty);
        DateTime? last = null;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            last = parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();

        return new LocalBackupSettings
        {
            IntervalHours = hours,
            LastBackupUtc = last
        };
    }

    public void SaveLocalBackupSettings(LocalBackupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string last = settings.LastBackupUtc is DateTime utc
            ? utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
            : string.Empty;
        Write(new Dictionary<string, string>
        {
            ["backup.intervalHours"] = LocalBackupSettings.Normalize(settings.IntervalHours).ToString(CultureInfo.InvariantCulture),
            ["backup.lastUtc"] = last
        });
    }

    public void SavePeripheralSettings(PeripheralSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Write(new Dictionary<string, string>
        {
            ["printer.transport"] = settings.PrinterTransport.ToString(),
            ["printer.windowsName"] = settings.WindowsPrinterName.Trim(),
            ["printer.serialPort"] = settings.PrinterSerialPort.Trim(),
            ["printer.baudRate"] = settings.PrinterBaudRate.ToString(CultureInfo.InvariantCulture),
            ["printer.host"] = settings.NetworkHost.Trim(),
            ["printer.port"] = settings.NetworkPort.ToString(CultureInfo.InvariantCulture),
            ["printer.openDrawer"] = settings.OpenDrawerOnCash ? "1" : "0",
            ["printer.drawerPin"] = settings.DrawerPin == 1 ? "1" : "0",
            ["scanner.mode"] = settings.ScannerMode.ToString(),
            ["scanner.serialPort"] = settings.ScannerSerialPort.Trim(),
            ["scanner.baudRate"] = settings.ScannerBaudRate.ToString(CultureInfo.InvariantCulture),
            ["scanner.addToCart"] = settings.ScannerAddToCart ? "1" : "0",
            ["scale.enabled"] = settings.ScaleEnabled ? "1" : "0",
            ["scale.serialPort"] = settings.ScaleSerialPort.Trim(),
            ["scale.baudRate"] = settings.ScaleBaudRate.ToString(CultureInfo.InvariantCulture)
        });
    }

    public ReportPreferences GetReportPreferences()
    {
        var values = ReadAll();
        string json = Get(values, "reports.view", string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new ReportPreferences();

        try
        {
            ReportPreferences? loaded = JsonSerializer.Deserialize<ReportPreferences>(json);
            if (loaded == null)
                return new ReportPreferences();

            loaded.Series ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            return loaded;
        }
        catch (JsonException)
        {
            return new ReportPreferences();
        }
    }

    public void SaveReportPreferences(ReportPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        Write(new Dictionary<string, string>
        {
            ["reports.view"] = JsonSerializer.Serialize(preferences)
        });
    }

    private Dictionary<string, string> ReadAll()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        using var connection = _dbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value FROM AppSettings;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            values[reader.GetString(0)] = reader.GetString(1);
        return values;
    }

    private void Write(Dictionary<string, string> values)
    {
        using var connection = _dbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        foreach (var pair in values)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO AppSettings (Key, Value) VALUES (@Key, @Value)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
                """;
            command.Parameters.AddWithValue("@Key", pair.Key);
            command.Parameters.AddWithValue("@Value", pair.Value);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) ? value : fallback;

    private static int ParseInt(string text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static TEnum ParseEnum<TEnum>(string text, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) ? value : fallback;

    private static TicketPaperWidth ParsePaper(string text) =>
        text == nameof(TicketPaperWidth.Mm80) || text == "80"
            ? TicketPaperWidth.Mm80
            : TicketPaperWidth.Mm58;
}
