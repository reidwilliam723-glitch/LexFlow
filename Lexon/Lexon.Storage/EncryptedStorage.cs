using Lexon.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lexon.Storage;

/// <summary>
/// Encrypted storage using AES-GCM with DPAPI-protected master key
/// </summary>
public class EncryptedStorage : IStorage
{
    private readonly string _storagePath;
    private readonly byte[] _masterKey;

    public EncryptedStorage(string storagePath)
    {
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        Directory.CreateDirectory(storagePath);
        _masterKey = GetOrCreateMasterKey();
    }

    public async Task SaveAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data);
        var encrypted = Encrypt(json, _masterKey);
        var filePath = GetFilePath(key);
        await File.WriteAllBytesAsync(filePath, encrypted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath)) return default;

        var encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var decrypted = Decrypt(encrypted, _masterKey);
        return JsonSerializer.Deserialize<T>(decrypted);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            await Task.Run(() => File.Delete(filePath), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(key);
        return await Task.Run(() => File.Exists(filePath), cancellationToken).ConfigureAwait(false);
    }

    private string GetFilePath(string key)
    {
        var safeKey = Convert.ToHexString(Encoding.UTF8.GetBytes(key)).ToLowerInvariant();
        return Path.Combine(_storagePath, $"{safeKey}.enc");
    }

    private byte[] GetOrCreateMasterKey()
    {
        var keyPath = Path.Combine(_storagePath, "master.key");
        if (File.Exists(keyPath))
        {
            var encryptedKey = File.ReadAllBytes(keyPath);
            return ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
        }

        var newKey = new byte[32]; // 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(newKey);

        var encrypted = ProtectedData.Protect(newKey, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, encrypted);
        return newKey;
    }

    private byte[] Encrypt(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[12]; // 96 bits for GCM
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes for GCM tag

        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

        return result;
    }

    private string Decrypt(byte[] encrypted, byte[] key)
    {
        var nonce = new byte[12];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes for GCM tag
        var ciphertext = new byte[encrypted.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(encrypted, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encrypted, nonce.Length, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(encrypted, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        var plaintext = new byte[ciphertext.Length];

        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
