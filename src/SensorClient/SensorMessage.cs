using System;

namespace SensorClient
{
    public class SensorMessage
    {
        public string SensorId { get; set; }
        public int MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Iv { get; set; }            
        public string Ciphertext { get; set; }    
        public string Signature { get; set; }    

        public SensorMessage(string sensorId, int messageId, DateTime timestamp, string iv, string ciphertext, string signature)
        {
            SensorId = sensorId;
            MessageId = messageId;
            Timestamp = timestamp;
            Iv = iv;
            Ciphertext = ciphertext;
            Signature = signature;
        }
    }
}