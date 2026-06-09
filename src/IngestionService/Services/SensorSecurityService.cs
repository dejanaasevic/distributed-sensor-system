using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using IngestionService.DTO;

namespace IngestionService.Services;

public interface ISensorSecurityService
{
    bool IsRateLimited(string sensorId);
    bool IsReplayAttack(string sensorId, int messageId, DateTime timestamp);
    bool VerifySignature(SensorMessageDto message, string publicKeyPem);
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

    public bool IsRateLimited(string sensorId)
    {
        var currentSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cacheKey = $"rate_{sensorId}_{currentSecond}";

        lock (LockObject)
        {
            if (!_cache.TryGetValue(cacheKey, out int requestCount))
            {
                _cache.Set(cacheKey, 1, TimeSpan.FromSeconds(2));
                return false;
            }

            if (requestCount >= MaxMessagesPerSecond)
            {
                return true;
            }

            _cache.Set(cacheKey, requestCount + 1, TimeSpan.FromSeconds(2));
            return false;
        }
    }

    public bool IsReplayAttack(string sensorId, int messageId, DateTime timestamp)
    {
        var timeDifference = Math.Abs((DateTime.UtcNow - timestamp.ToUniversalTime()).TotalSeconds);
        if (timeDifference > AllowedClockSkewSeconds)
        {
            return true;
        }

        var cacheKey = $"msg_{sensorId}_{messageId}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(2));
        return false;
    }

    public bool VerifySignature(SensorMessageDto message, string publicKeyPem)
    {
        try
        {
            if (string.IsNullOrEmpty(publicKeyPem)) return false;

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            string rawData = $"{message.SensorId}:{message.Temperature}:{message.Timestamp:O}:{message.MessageId}";
            byte[] dataBytes = Encoding.UTF8.GetBytes(rawData);
            byte[] signatureBytes = Convert.FromBase64String(message.Signature);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}