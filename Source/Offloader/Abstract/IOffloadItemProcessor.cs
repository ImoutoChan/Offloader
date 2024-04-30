namespace Offloader;

public interface IOffloadItemProcessor<in T>
{
    Task ProcessAsync(T item, CancellationToken ct);
}
