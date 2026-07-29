using System.Security.Cryptography;

using UserManagementPoC.Shared.Security.Contracts;

namespace UserManagementPoC.Shared.Security.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    public AesEncryptionService(IKeyVaultService keyVaultService)
    {
        var keyString = keyVaultService.GetSecretAsync("EncryptionKey").GetAwaiter().GetResult() ?? throw new InvalidOperationException("Encryption key not found in vault.");
        _key = Convert.FromBase64String(keyString);
        if (_key.Length != 32) throw new InvalidOperationException($"Encryption key must be 32 bytes (base64-encoded). Got {_key.Length} bytes.");

    }
    public string Encrypt(string plainText, out string ivBase64)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        ivBase64 = Convert.ToBase64String(aes.IV);
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());

    }
    public string Decrypt(string cipherText, string ivBase64)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = Convert.FromBase64String(ivBase64);
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();

    }
}