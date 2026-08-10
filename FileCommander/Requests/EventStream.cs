using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Channels;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Storage.Streams;

public class ProducerConsumerRandomAccessStream : IRandomAccessStream
{
    private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>();
    private bool _completed = false;
    private ulong _position = 0;

    public bool CanRead => true;
    public bool CanWrite => true;
    public ulong Position => _position;
    public ulong Size { get => _position; set => throw new NotSupportedException(); }

    public void Dispose() => _channel.Writer.TryComplete();
    public IRandomAccessStream CloneStream() => throw new NotSupportedException();
    public IInputStream GetInputStreamAt(ulong position) => this;
    public IOutputStream GetOutputStreamAt(ulong position) => throw new NotSupportedException();

    public void Seek(ulong position)
    {
        // Allow Seek(0) at start, ignore Seek to current position
        if ((position == 0 && _position == 0) || position == _position)
            return;

        throw new NotSupportedException("Random seeking not supported in live stream.");
    }

    // Correct WinRT signature for ReadAsync
    //public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    //{
    //    return AsyncInfo.Run<IBuffer, uint>(async (ct, progress) =>
    //    {
    //        // Try immediate read first
    //        if (_channel.Reader.TryRead(out var chunk))
    //        {
    //            var dataWriter = new DataWriter();
    //            dataWriter.WriteBytes(chunk);
    //            var outBuffer = dataWriter.DetachBuffer();
    //            _position += (ulong)chunk.Length;
    //            progress.Report((uint)chunk.Length);
    //            return outBuffer;
    //        }

    //        // Wait for data, but don't block forever
    //        if (await _channel.Reader.WaitToReadAsync(ct))
    //        {
    //            if (_channel.Reader.TryRead(out var chunk2))
    //            {
    //                var dataWriter = new DataWriter();
    //                dataWriter.WriteBytes(chunk2);
    //                var outBuffer = dataWriter.DetachBuffer();
    //                _position += (ulong)chunk2.Length;
    //                progress.Report((uint)chunk2.Length);
    //                return outBuffer;
    //            }
    //        }

    //        // No data available — return empty buffer
    //        return new Windows.Storage.Streams.Buffer(0);
    //    });
    //}

    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        return AsyncInfo.Run<IBuffer, uint>(async (ct, progress) =>
        {
            while (true)
            {
                // If data is available immediately, send it
                if (_channel.Reader.TryRead(out var chunk))
                {
                    var dataWriter = new DataWriter();
                    dataWriter.WriteBytes(chunk);
                    var outBuffer = dataWriter.DetachBuffer();
                    _position += (ulong)chunk.Length;
                    progress.Report((uint)chunk.Length);
                    return outBuffer;
                }

                // If stream is complete and no more data, return empty buffer (EOF)
                if (_completed && _channel.Reader.Count == 0)
                {
                    return new Windows.Storage.Streams.Buffer(0);
                }

                // Wait for new data or completion
                if (!await _channel.Reader.WaitToReadAsync(ct))
                {
                    // Channel closed without data
                    return new Windows.Storage.Streams.Buffer(0);
                }
            }
        });
    }
    
    // Required by IOutputStream (not used here)
    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        throw new NotSupportedException();
    }

    public IAsyncOperation<bool> FlushAsync()
    {
        return AsyncInfo.Run<bool>(async (ct) => await Task.FromResult(true));
    }

    // Push NDJSON data into the stream
    public async Task PushAsync(byte[] data)
    {
        if (!_completed)
        {
            await _channel.Writer.WriteAsync(data);
        }
    }

    // Signal end of stream
    public void Complete()
    {
        _completed = true;
        _channel.Writer.TryComplete();
    }
}


public static class BufferExtensions
{
    public static void CopyTo(this byte[] source, IBuffer buffer)
    {
        using var dataWriter = new DataWriter();
        dataWriter.WriteBytes(source);
        var written = dataWriter.DetachBuffer();
        buffer.Length = written.Length;
        using var dataReader = DataReader.FromBuffer(written);
        dataReader.ReadBytes(source);
    }
}