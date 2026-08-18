using System.Collections.Generic;
using WarHack.Infrastructure.Models;


public interface IConfigurationStore
{
    /// <summary>
    /// Gets setting value by key.
    /// </summary>
    /// <param name="section">config section name</param>
    /// <param name="key">The key of the config section</param>
    /// <returns>String</returns>
    
    //other try
    //string GetConfiguration<T>() where T : new();
    string GetSectionKV();
}

interface IAthenaeum
{
    
}


