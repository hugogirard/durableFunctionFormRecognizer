using System;

public class CollectorOptions : BaseOptions
{    
    public int BatchSize { get; set; }
    public int MinBacklogSize { get; set; }
    public TimeSpan CollectDelay { get; set; }    
}