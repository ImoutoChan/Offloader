namespace Offloader;

public interface IOffloader<in T>
{
    Task OffloadAsync(T vote);
}
