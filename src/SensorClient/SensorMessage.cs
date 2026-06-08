using SensorClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorClient
{
    public class SensorMessage
    {
        public string SensorId { get; set; }
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
        public int AlarmPriority { get; set; }
        public DataQuality Quality { get; set; }
        public int MessageId { get; set; }
        public string Signature { get; set; }

        public SensorMessage(string sensorId, double temperature, DateTime timestamp, int alarmPriority, DataQuality quality, int messageId, string signature)
        {
            SensorId = sensorId;
            Temperature = temperature;
            Timestamp = timestamp;
            AlarmPriority = alarmPriority;
            Quality = quality;
            MessageId = messageId;
            Signature = signature;
        }
    }
}