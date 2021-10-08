using System.Linq;
using System.Collections.Generic;

public static class EnumerableHelper
{        
    public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int partitionSize)
    {
        while (source.Any())
        {
            yield return source.Take(partitionSize);
            source = source.Skip(partitionSize);
        }
    }
}