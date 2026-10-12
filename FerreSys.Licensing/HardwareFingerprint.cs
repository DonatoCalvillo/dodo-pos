using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace FerreSys.Licensing;

public static class HardwareFingerprint
{
    public static string GetDeviceId()
    {
        string processorId = GetWmiProperty("Win32_Processor", "ProcessorId");
        string motherboardId = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
        string raw = $"{processorId}-{motherboardId}";

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..16];
    }

    private static string GetWmiProperty(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            using ManagementObjectCollection results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                    return obj[property]?.ToString() ?? string.Empty;
            }
        }
        catch
        {
        }

        return "UNKNOWN_HW";
    }
}
