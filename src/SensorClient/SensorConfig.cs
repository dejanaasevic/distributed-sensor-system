using System;
using System.Security.Cryptography;

namespace SensorClient
{
    public class SensorConfig
    {
        public string Id { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public double AlarmThreshold1 { get; set; }
        public double AlarmThreshold2 { get; set; }
        public double AlarmThreshold3 { get; set; }
        public DataQuality Quality { get; set; }

        public string SymmetricKey { get; set; }     
        public string PublicKeyXml { get; set; }      
        public string PrivateKeyXml { get; set; }     

        public SensorConfig(string id, double minTemperature, double maxTemperature, double alarmThreshold1, double alarmThreshold2, double alarmThreshold3, DataQuality quality)
        {
            Id = id;
            MinTemperature = minTemperature;
            MaxTemperature = maxTemperature;
            AlarmThreshold1 = alarmThreshold1;
            AlarmThreshold2 = alarmThreshold2;
            AlarmThreshold3 = alarmThreshold3;
            Quality = quality;
 
            byte[] aesKeyBytes = new byte[32]; // 256 bits
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(aesKeyBytes);
            }
            SymmetricKey = Convert.ToBase64String(aesKeyBytes);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                PrivateKeyXml = rsa.ToXmlString(true);  // Includes private key parameters
                PublicKeyXml = rsa.ToXmlString(false); // Public key parameter only
            }
        }
    }
}