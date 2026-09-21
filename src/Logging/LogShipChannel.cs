using System.Threading.Channels;

namespace WarHack.Logging
{
    public sealed class LogShipChannel
    {
        public const int Capacity = 2048;
        public const int PressureThreshold = Capacity * 9 / 10; // 90% of capacity, used for health check

        private readonly Channel<LogQueue> _channel = Channel.CreateBounded<LogQueue>(
            new BoundedChannelOptions(capacity: Capacity)
            {
                // Never block the request thread, drop the oldest queued row
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

        public ChannelWriter<LogQueue> Writer => _channel.Writer;
        public ChannelReader<LogQueue> Reader => _channel.Reader;

        /// <summary>
        /// checks channel count for health endpoint
        /// near <see cref="Capacity"/> means rows are being dropped
        /// </summary>
        public int QueueDepth => _channel.Reader.CanCount ? _channel.Reader.Count : -1;
    }
}
