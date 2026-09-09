using System.Threading.Channels;

namespace WarHack.Logging
{
    public sealed class LogShipChannel
    {
        private readonly Channel<LogQueue> _channel = Channel.CreateBounded<LogQueue>(
            new BoundedChannelOptions(capacity: 2048)
            {
                // Never block the request thread, drop the oldest queued row.
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

        public ChannelWriter<LogQueue> Writer => _channel.Writer;
        public ChannelReader<LogQueue> Reader => _channel.Reader;
    }
}
