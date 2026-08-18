public interface ILogShipHarbor
{
    void PrintToOut(string message);
}

public class Logship : ILogShipHarbor
{

    private readonly ILogger<Logship> _logger;

    public Logship(ILogger<Logship> logger) =>
        _logger = logger;

    public void PrintToOut(string message) =>
        _logger.LogInformation(message);
}