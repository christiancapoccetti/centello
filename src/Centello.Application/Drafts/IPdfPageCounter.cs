namespace Centello.Application;

public interface IPdfPageCounter
{
    Task<Result<int>> Count(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
}
