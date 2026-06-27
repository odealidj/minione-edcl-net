using System.Threading.Channels;
using EDCL.Module.Cargo.Domain.Entities;

namespace EDCL.Module.Cargo.Infrastructure.Channels;

public class IngestionErrorChannel
{
    private readonly Channel<IngestionError> _channel;

    public IngestionErrorChannel()
    {
        // Unbounded channel since we don't expect millions of errors per second 
        // and we want to ensure all connected clients receive the notification.
        // For production, Bounded might be safer depending on memory constraints.
        _channel = Channel.CreateUnbounded<IngestionError>();
    }

    public async Task PublishErrorAsync(IngestionError error, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(error, cancellationToken);
    }

    public IAsyncEnumerable<IngestionError> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
