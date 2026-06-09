using System.Collections.Concurrent;

namespace IngestionService.Services
{
    public interface ISensorBlockManager
    {
        bool IsBlocked(string sensorId);
        bool RecordRequestAndCheckBlock(string sensorId);
        void BlockSensor(string sensorId, int durationSeconds);
    }

    public class SensorBlockManager : ISensorBlockManager
    {
        private readonly ConcurrentDictionary<string, DateTime> _blockedSensors = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestTimestamps = new();

        public bool IsBlocked(string sensorId)
        {
            if (_blockedSensors.TryGetValue(sensorId, out var blockedUntil))
            {
                if (DateTime.UtcNow < blockedUntil)
                {
                    return true;
                }
                _blockedSensors.TryRemove(sensorId, out _);
            }
            return false;
        }

        public bool RecordRequestAndCheckBlock(string sensorId)
        {
            if (IsBlocked(sensorId))
            {
                return true;
            }

            var now = DateTime.UtcNow;
            var queue = _requestTimestamps.GetOrAdd(sensorId, _ => new ConcurrentQueue<DateTime>());

            queue.Enqueue(now);

            // Clean up timestamps older than 1 second
            var oneSecondAgo = now.AddSeconds(-1);
            while (queue.TryPeek(out var timestamp) && timestamp < oneSecondAgo)
            {
                queue.TryDequeue(out _);
            }

            // If a sensor sends more than 10 messages in 1 second, block it for 30 seconds
            if (queue.Count > 10)
            {
                BlockSensor(sensorId, 30);
                return true;
            }

            return false;
        }

        public void BlockSensor(string sensorId, int durationSeconds)
        {
            _blockedSensors[sensorId] = DateTime.UtcNow.AddSeconds(durationSeconds);
        }
    }
}
