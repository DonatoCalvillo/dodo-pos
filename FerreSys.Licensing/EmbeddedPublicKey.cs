using System.Reflection;

namespace FerreSys.Licensing;

public static class EmbeddedPublicKey
{
    public static string Xml
    {
        get
        {
            var assembly = typeof(EmbeddedPublicKey).Assembly;
            const string name = "FerreSys.Licensing.Keys.PublicKey.xml";
            using Stream stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("Falta la llave pública embebida.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }
    }
}
