using System.Threading.Channels;

namespace Offloader.Implementation;

internal interface IOffloadReader<T>
{
    ChannelReader<T> Reader { get; }
    
    void Complete();
}
