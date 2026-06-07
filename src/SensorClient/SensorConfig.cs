using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public SensorConfig(string id, double minTemperature, double maxTemperature, double alarmThreshold1, double alarmThreshold2, double alarmThreshold3, DataQuality quality)
        {
            Id = id;
            MinTemperature = minTemperature;
            MaxTemperature = maxTemperature;
            AlarmThreshold1 = alarmThreshold1;
            AlarmThreshold2 = alarmThreshold2;
            AlarmThreshold3 = alarmThreshold3;
            Quality = quality;
        }
    }
}
