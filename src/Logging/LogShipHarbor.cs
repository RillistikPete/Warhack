namespace WarHack.Logging
{
    public sealed class LogShipHarbor : ILogShipHarbor
    {
        private readonly ILogger<LogShipHarbor> _logger;
        private readonly LogShipChannel _channel;

        public LogShipHarbor(ILogger<LogShipHarbor> logger, LogShipChannel channel)
        {
            _logger = logger;
            _channel = channel;
        }

        public void Ship(string message, string level = "Information", string? logger = null)
        {
            var entry = new LogQueue
            {
                DateAdded = DateTimeOffset.UtcNow,
                Level = level,
                Message = message,
                ProjectName = "WarHack",
                Logger = logger ?? nameof(LogShipHarbor)
            };

            // attempt to write entry to channel
            bool tryWrite = _channel.Writer.TryWrite(entry);


            // if entry is not written to channel
            if (tryWrite == false)
            {
                _logger.LogWarning("Log ship channel closed; row not queued: {Message}", message);
            }
            // had this syntax but it reads more confusing
            // if (!_channel.Writer.TryWrite(entry)){
        }
    }
}
