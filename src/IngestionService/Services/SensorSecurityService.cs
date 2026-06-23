using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using IngestionService.DTO;

namespace IngestionService.Services;

public interface ISensorSecurityService
{
    bool IsReplayAttack(string sensorId, int messageId, DateTime timestamp);
    bool VerifySignature(SensorMessageDto message, string publicKeyPem);
    DecryptedPayloadDTO? DecryptMessage(SensorMessageDto encryptedDto, string aesKeyBase64);
}

public class SensorSecurityService : ISensorSecurityService
{
    private readonly IMemoryCache _cache;
    private static readonly object LockObject = new();
    private const int MaxMessagesPerSecond = 10;
    private const int AllowedClockSkewSeconds = 5;

    public SensorSecurityService(IMemoryCache cache)
    {
        _cache = cache;
    }


    public bool IsReplayAttack(string sensorId, int messageId, DateTime timestamp)
    {
        var timeDifference = Math.Abs((DateTime.UtcNow - timestamp.ToUniversalTime()).TotalSeconds);
        if (timeDifference > AllowedClockSkewSeconds)
        {
            return true;
        }

        var lastIdKey = $"last_id_{sensorId}";
        if (_cache.TryGetValue(lastIdKey, out int lastMessageId))
        {
            if (messageId <= lastMessageId)
            {
                return true; 
            }
        }

        _cache.Set(lastIdKey, messageId, TimeSpan.FromDays(1));
        return false;
    }

    public bool VerifySignature(SensorMessageDto message, string publicKeyPem)
    {
        Console.WriteLine("Verifying signature");
        try
        {
            if (string.IsNullOrEmpty(publicKeyPem)) return false;

            Console.WriteLine("passed public key check");

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyPem);
            string formattedTime = message.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            string rawData = $"{message.SensorId}:{message.MessageId}:{formattedTime}:{message.Ciphertext}";

            byte[] dataBytes = Encoding.UTF8.GetBytes(rawData);
            byte[] signatureBytes = Convert.FromBase64String(message.Signature);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Signature verification failed due to an exception: {ex.Message}");
            return false;
        }
    }

    public DecryptedPayloadDTO? DecryptMessage(SensorMessageDto encryptedDto, string aesKeyBase64)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedDto.Ciphertext) || string.IsNullOrEmpty(encryptedDto.Iv))
                return null;

            byte[] ciphertext = Convert.FromBase64String(encryptedDto.Ciphertext);
            byte[] iv = Convert.FromBase64String(encryptedDto.Iv);
            byte[] key = Convert.FromBase64String(aesKeyBase64);

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(ciphertext);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            string decryptedJson = sr.ReadToEnd();

            return JsonSerializer.Deserialize<DecryptedPayloadDTO>(decryptedJson);
        }
        catch
        {
            return null;
        }
    }
}