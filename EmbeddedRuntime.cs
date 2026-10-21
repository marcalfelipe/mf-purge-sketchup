using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace MFPurgeSketchUp;

internal static class EmbeddedRuntime
{
    private const string ResourceName = "MFPurgeSketchUp.embedded_runtime.zip";
    private static readonly object Sync = new();

    public static string GetWorkerExecutable()
    {
        lock (Sync)
        {
            var hash = GetPayloadHash();
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MF Purge SketchUp",
                "runtime",
                hash[..16]);
            var worker = Path.Combine(root, "worker", "MFPurgeWorkerConsole.exe");
            var api = Path.Combine(root, "worker", "SketchUpAPI.dll");
            var preferences = Path.Combine(root, "worker", "SketchUpCommonPreferences.dll");

            if (File.Exists(worker) && File.Exists(api) && File.Exists(preferences))
                return worker;

            Directory.CreateDirectory(root);
            using var payload = OpenPayload();
            ZipFile.ExtractToDirectory(payload, root, overwriteFiles: true);

            if (!File.Exists(worker) || !File.Exists(api) || !File.Exists(preferences))
                throw new InvalidOperationException("O runtime interno não pôde ser extraído corretamente.");

            return worker;
        }
    }

    private static string GetPayloadHash()
    {
        using var payload = OpenPayload();
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    private static Stream OpenPayload() =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
        ?? throw new InvalidOperationException("O runtime interno não foi incluído no executável.");
}
