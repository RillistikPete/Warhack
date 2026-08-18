namespace WarHack;

public class Intercessors : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<OptionsQueryMiddleware>();
            next(builder);
        };
    }
}