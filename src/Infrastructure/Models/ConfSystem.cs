using Microsoft.Extensions.Options;

internal class ConfSystem
{
    private HomeControllerSettings _homeCs;
    public ConfSystem(IOptions<HomeControllerSettings> hcsOptions)
    {
        _homeCs = hcsOptions.Value;
    }
}