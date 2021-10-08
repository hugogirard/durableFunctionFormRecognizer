using System;

public class ProcessorOptions : BaseOptions
{        
    public int MaxRetries { get; set; }
    public string FormRecognizerModelId { get; set; }
    public TimeSpan NoDataDelay { get; set; }
}