using System;

public abstract class BaseOptions
{    
    public int NbPartitions { get; set; }
    public int PartitionSize { get; set; } 
    public string BlobContainerName { get; set; }
}