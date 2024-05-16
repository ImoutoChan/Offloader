using System.Threading.Channels;

namespace Offloader.Implementation;

/// <remarks>
/// Should be registered as a singleton.
/// </remarks>>
internal class Offloader<T> : IOffloader<T>, IOffloadReader<T>
{
    private Channel<T> OffloadedItems { get; }
        = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async Task OffloadAsync(T item) => await OffloadedItems.Writer.WriteAsync(item);

    public ChannelReader<T> Reader => OffloadedItems.Reader;
    
    public void Complete() => OffloadedItems.Writer.Complete();
}
