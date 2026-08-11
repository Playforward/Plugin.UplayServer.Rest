using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Plugin.UplayServer.Rest;

/// <summary>
/// Reads the public half of the local RendezVous signing identity. The file
/// format is shared by REST and Quazal:
/// </summary>
internal static class RendezVousIdentity
{
    public static bool TryGetEcs1PublicKey(string configuredPath, out string publicKey, out string error)
    {
        publicKey = "";
        error = "";
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            error = "identity file is not configured";
            return false;
        }

        try
        {
            string path = Path.IsPathFullyQualified(configuredPath)
                ? configuredPath
                : Path.Combine(Directory.GetCurrentDirectory(), configuredPath);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("EcdsaP256PrivateKeyPkcs8", out JsonElement value) ||
                value.GetString() is not string encoded || string.IsNullOrWhiteSpace(encoded))
            {
                error = $"{path} has no EcdsaP256PrivateKeyPkcs8 value";
                return false;
            }

            byte[] privateKey = Convert.FromBase64String(encoded);
            using ECDsa signer = ECDsa.Create();
            signer.ImportPkcs8PrivateKey(privateKey, out int consumed);
            if (consumed != privateKey.Length)
                throw new CryptographicException("PKCS#8 identity contains trailing bytes");

            ECParameters parameters = signer.ExportParameters(includePrivateParameters: false);
            if (parameters.Q.X is not { Length: 32 } x || parameters.Q.Y is not { Length: 32 } y)
                throw new CryptographicException("RendezVous identity is not P-256");

            byte[] blob = new byte[72];
            "ECS1"u8.CopyTo(blob);
            BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(4), 32);
            x.CopyTo(blob, 8);
            y.CopyTo(blob, 40);
            publicKey = Convert.ToBase64String(blob);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or CryptographicException or JsonException)
        {
            error = exception.Message;
            return false;
        }
    }
}
