using Centello.Application;

namespace Centello.Application.Tests;

public sealed class PdfFileReader_should_expected_behavior
{
    [Theory]
    [InlineData(24)]
    [InlineData(DocumentLimits.MaximumPdfBytes)]
    public async Task Read_a_non_seekable_pdf_within_the_limit(int bytes)
    {
        using var stream = new ProviderStream(bytes);

        var result = await PdfFileReader.Read(stream);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(bytes, result.Value.Length);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(result.Value, 0, 5));
    }

    [Fact]
    public async Task Read_at_most_one_byte_past_the_limit_from_a_non_seekable_provider()
    {
        using var stream = new ProviderStream(DocumentLimits.MaximumPdfBytes * 2);

        var result = await PdfFileReader.Read(stream);

        Assert.Equal(PhotoErrors.PdfTooLarge, result.Error);
        Assert.Equal(DocumentLimits.MaximumPdfBytes + 1, stream.BytesRead);
    }

    private sealed class ProviderStream(int bytes) : Stream
    {
        public int BytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var count = Math.Min(Math.Min(buffer.Length, bytes - BytesRead), 8192);
            buffer.Span[..count].Clear();
            if (BytesRead == 0)
                "%PDF-"u8.CopyTo(buffer.Span);

            BytesRead += count;
            return ValueTask.FromResult(count);
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
