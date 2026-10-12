using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FerreSys.Domain;

namespace FerreSys.Licensing;

public static class LicenseSigner
{
    public static SignedLicenseContainer Sign(LicensePayload payload, string privateKeyXml)
    {
        string json = JsonSerializer.Serialize(payload, LicenseJson.Options);
        using var rsa = RSA.Create();
        rsa.FromXmlString(privateKeyXml);
        byte[] signature = rsa.SignData(
            Encoding.UTF8.GetBytes(json),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return new SignedLicenseContainer
        {
            JsonPayload = json,
            Signature = Convert.ToBase64String(signature)
        };
    }

    public static (string PrivateXml, string PublicXml) CreateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ToXmlString(true), rsa.ToXmlString(false));
    }
}
