using System.Drawing.Printing;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FerreSys.Infrastructure.UI.Printing;

internal static partial class PeripheralDiscovery
{
    public const string MissingDriverSuffix = " (controlador no disponible)";

    public static readonly int[] BaudRates = [9600, 19200, 38400, 57600, 115200];

    public static IReadOnlyList<string> InstalledPrinters()
    {
        return PrinterSettings.InstalledPrinters
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> PrinterChoices()
    {
        var installed = InstalledPrinters();
        var names = new List<string>(installed);
        foreach (string device in PrinterDevicesWithoutQueue(installed))
            names.Add(device + MissingDriverSuffix);
        foreach ((string port, string device) in BluetoothPrinters())
        {
            if (installed.Any(queue => queue.Contains(device, StringComparison.OrdinalIgnoreCase)))
                continue;
            names.Add(BluetoothChoice(device, port));
        }

        return names;
    }

    public static bool HasMissingDriver(string? printerName) =>
        !string.IsNullOrWhiteSpace(printerName)
        && printerName.EndsWith(MissingDriverSuffix, StringComparison.Ordinal);

    public static bool TryGetBluetoothPort(string? choice, out string port)
    {
        port = string.Empty;
        if (string.IsNullOrWhiteSpace(choice))
            return false;

        Match match = BluetoothChoicePattern().Match(choice.Trim());
        if (!match.Success)
            return false;

        port = match.Groups[1].Value.ToUpperInvariant();
        return true;
    }

    public static string PortNameOf(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return string.Empty;

        int split = label.IndexOf(" — ", StringComparison.Ordinal);
        return (split > 0 ? label[..split] : label).Trim();
    }

    public static IReadOnlyList<string> SerialPortChoices()
    {
        Dictionary<string, string> bluetooth = BluetoothPrinters()
            .GroupBy(item => item.Port, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Device, StringComparer.OrdinalIgnoreCase);

        return SerialPort.GetPortNames()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(port => bluetooth.TryGetValue(port, out string? device) ? $"{port} — {device}" : port)
            .ToList();
    }

    private static string BluetoothChoice(string device, string port) => $"{device} (Bluetooth {port})";

    [GeneratedRegex(@"\(Bluetooth (COM\d+)\)$", RegexOptions.IgnoreCase)]
    private static partial Regex BluetoothChoicePattern();

    private static IReadOnlyList<string> PrinterDevicesWithoutQueue(IReadOnlyList<string> installed)
    {
        var found = new List<string>();
        Guid printerClass = new("4d36e979-e325-11ce-bfc1-08002be10318");
        IntPtr set = SetupDiGetClassDevsW(ref printerClass, null, IntPtr.Zero, DigcfPresent);
        if (set == InvalidHandle)
            return found;

        try
        {
            var data = new SpDevInfoData { cbSize = (uint)Marshal.SizeOf<SpDevInfoData>() };
            for (uint index = 0; SetupDiEnumDeviceInfo(set, index, ref data); index++)
            {
                string? name = ReadDeviceName(set, ref data);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (installed.Any(queue => queue.Contains(name, StringComparison.OrdinalIgnoreCase)
                        || name.Contains(queue, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (found.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                found.Add(name);
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }

        found.Sort(StringComparer.OrdinalIgnoreCase);
        return found;
    }

    private static string? ReadDeviceName(IntPtr set, ref SpDevInfoData data)
    {
        string? friendly = ReadRegistryString(set, ref data, SpdrpFriendlyName);
        return string.IsNullOrWhiteSpace(friendly)
            ? ReadRegistryString(set, ref data, SpdrpDeviceDesc)
            : friendly;
    }

    private static string? ReadRegistryString(IntPtr set, ref SpDevInfoData data, uint property)
    {
        SetupDiGetDeviceRegistryPropertyW(set, ref data, property, out _, null, 0, out uint required);
        if (required < 2)
            return null;

        var buffer = new byte[required];
        if (!SetupDiGetDeviceRegistryPropertyW(set, ref data, property, out _, buffer, (uint)buffer.Length, out _))
            return null;

        return Encoding.Unicode.GetString(buffer).TrimEnd('\0').Trim();
    }

    private const uint DigcfPresent = 0x02;
    private const uint SpdrpDeviceDesc = 0x00;
    private const uint SpdrpFriendlyName = 0x0C;
    private static readonly IntPtr InvalidHandle = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDevInfoData
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryPropertyW(
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[]? propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInstanceIdW(
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        StringBuilder deviceInstanceId,
        int deviceInstanceIdSize,
        out int requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    private static IReadOnlyList<(string Port, string Device)> BluetoothPrinters()
    {
        var namesByAddress = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DeviceSnapshot device in EnumeratePresent(new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974")))
        {
            string? address = BluetoothAddress(device.InstanceId);
            if (address == null || string.IsNullOrWhiteSpace(device.Name))
                continue;
            namesByAddress.TryAdd(address, device.Name);
        }

        var found = new List<(string Port, string Device)>();
        foreach (DeviceSnapshot device in EnumeratePresent(new Guid("4d36e978-e325-11ce-bfc1-08002be10318")))
        {
            string? address = BluetoothAddress(device.InstanceId);
            if (address == null || !namesByAddress.TryGetValue(address, out string? name))
                continue;

            Match port = ComPortPattern().Match(device.Name);
            if (!port.Success)
                continue;
            string com = port.Groups[1].Value.ToUpperInvariant();
            if (found.Any(item => string.Equals(item.Port, com, StringComparison.OrdinalIgnoreCase)))
                continue;
            found.Add((com, name));
        }

        return found;
    }

    private static string? BluetoothAddress(string instanceId)
    {
        foreach (Match match in BluetoothAddressPattern().Matches(instanceId))
        {
            if (match.Index > 0 && instanceId[match.Index - 1] == '-')
                continue;
            string address = match.Groups[1].Value.ToUpperInvariant();
            if (address == "000000000000")
                continue;
            return address;
        }

        return null;
    }

    [GeneratedRegex(@"(?<![0-9A-F])([0-9A-F]{12})(?![0-9A-F])", RegexOptions.IgnoreCase)]
    private static partial Regex BluetoothAddressPattern();

    [GeneratedRegex(@"\((COM\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex ComPortPattern();

    private readonly record struct DeviceSnapshot(string Name, string InstanceId);

    private static List<DeviceSnapshot> EnumeratePresent(Guid classGuid)
    {
        var found = new List<DeviceSnapshot>();
        IntPtr set = SetupDiGetClassDevsW(ref classGuid, null, IntPtr.Zero, DigcfPresent);
        if (set == InvalidHandle)
            return found;

        try
        {
            var data = new SpDevInfoData { cbSize = (uint)Marshal.SizeOf<SpDevInfoData>() };
            for (uint index = 0; SetupDiEnumDeviceInfo(set, index, ref data); index++)
            {
                string? name = ReadDeviceName(set, ref data);
                string? instanceId = ReadInstanceId(set, ref data);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(instanceId))
                    continue;
                found.Add(new DeviceSnapshot(name, instanceId));
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }

        return found;
    }

    private static string? ReadInstanceId(IntPtr set, ref SpDevInfoData data)
    {
        var buffer = new StringBuilder(512);
        if (!SetupDiGetDeviceInstanceIdW(set, ref data, buffer, buffer.Capacity, out _))
            return null;
        return buffer.ToString();
    }
}
