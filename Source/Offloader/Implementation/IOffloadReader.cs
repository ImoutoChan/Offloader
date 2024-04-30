using System.Threading.Channels;

namespace Offloader;

internal interface IOffloadReader<T>
{
    ChannelReader<T> Reader { get; }
    
    void Complete();
}
