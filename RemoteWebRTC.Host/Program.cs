using System.Threading.Channels;
using SIPSorceryMedia.Abstractions;
using SIPSorcery.Net;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace RemoteWebRTC.Host;

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("RemoteWebRTC host starting...");

        await using var streamer = new WgcToWebRtcStreamer();
        await streamer.StartAsync();

        Console.WriteLine("Press ENTER to stop.");
        Console.ReadLine();

        await streamer.StopAsync();
    }
}

internal sealed class WgcToWebRtcStreamer : IAsyncDisposable
{
    private readonly RTCPeerConnection _peerConnection;
    private readonly MediaStreamTrack _videoTrack;
    private readonly DummyNvencBridge _nvencBridge;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly object _logLock = new();

    private GraphicsCaptureSession? _captureSession;
    private Direct3D11CaptureFramePool? _framePool;

    private Channel<RawFrame>? _frameChannel;
    private CancellationTokenSource? _streamCts;
    private Task? _frameProducerTask;
    private Task? _frameConsumerTask;
    private volatile int _isStarted;

    public WgcToWebRtcStreamer()
    {
        var config = new RTCConfiguration
        {
            iceServers =
            [
                new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
            ]
        };

        _peerConnection = new RTCPeerConnection(config);
        _nvencBridge = new DummyNvencBridge();

        _videoTrack = new MediaStreamTrack(
            [new VideoFormat(VideoCodecsEnum.H264, 96, 90000, "packetization-mode=1;profile-level-id=42e01f")],
            MediaStreamStatusEnum.SendOnly);

        _peerConnection.addTrack(_videoTrack);

        _nvencBridge.OnEncodedAccessUnit += (durationRtp, payload) => _peerConnection.SendVideo(durationRtp, payload);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (Interlocked.CompareExchange(ref _isStarted, 1, 0) == 1)
            {
                Log("StartAsync ignored; already started.");
                return;
            }

            _streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _frameChannel = Channel.CreateBounded<RawFrame>(new BoundedChannelOptions(2)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

            _frameConsumerTask = RunEncoderLoopAsync(_frameChannel.Reader, _streamCts.Token);

            if (!OperatingSystem.IsWindows())
            {
                Log("Running on non-Windows runtime; using bounded dummy frame producer.");
                _frameProducerTask = RunDummyProducerLoopAsync(_frameChannel.Writer, _streamCts.Token);
                return;
            }

            if (!GraphicsCaptureSession.IsSupported())
            {
                Log("Windows.Graphics.Capture not supported in this runtime context; using bounded dummy frame producer.");
                _frameProducerTask = RunDummyProducerLoopAsync(_frameChannel.Writer, _streamCts.Token);
                return;
            }

            var item = CaptureItemFactory.TryCreatePrimaryDisplayItem();
            var device = CaptureDeviceFactory.CreateD3DDevice();

            if (item is null || device is null)
            {
                Log("Capture item or D3D device unavailable; using bounded dummy frame producer.");
                _frameProducerTask = RunDummyProducerLoopAsync(_frameChannel.Writer, _streamCts.Token);
                return;
            }

            _framePool = Direct3D11CaptureFramePool.Create(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                item.Size);

            _captureSession = _framePool.CreateCaptureSession(item);
            _framePool.FrameArrived += OnFrameArrived;
            _captureSession.StartCapture();
            Log("Windows capture session started.");
        }
        catch
        {
            Interlocked.Exchange(ref _isStarted, 0);
            throw;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (Interlocked.CompareExchange(ref _isStarted, 0, 1) == 0)
            {
                Log("StopAsync ignored; streamer already stopped.");
                return;
            }

            _framePool?.FrameArrived -= OnFrameArrived;

            if (_streamCts is not null)
            {
                await _streamCts.CancelAsync();
            }

            if (_frameChannel is not null)
            {
                _frameChannel.Writer.TryComplete();
            }

            await AwaitLoopAsync(_frameProducerTask);
            await AwaitLoopAsync(_frameConsumerTask);

            _captureSession?.Dispose();
            _framePool?.Dispose();
            _streamCts?.Dispose();
            _peerConnection.Close("Host stopped");

            _captureSession = null;
            _framePool = null;
            _streamCts = null;
            _frameChannel = null;
            _frameProducerTask = null;
            _frameConsumerTask = null;

            Log("Streamer stopped.");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task RunDummyProducerLoopAsync(ChannelWriter<RawFrame> writer, CancellationToken cancellationToken)
    {
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));

        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                writer.TryWrite(RawFrame.Dummy());
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task RunEncoderLoopAsync(ChannelReader<RawFrame> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var frame in reader.ReadAllAsync(cancellationToken))
            {
                _nvencBridge.EncodeSurface(frame.Surface, frame.Width, frame.Height);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        var localWriter = _frameChannel?.Writer;
        if (localWriter is null)
        {
            return;
        }

        using var frame = sender.TryGetNextFrame();
        if (frame is null)
        {
            return;
        }

        localWriter.TryWrite(new RawFrame(frame.Surface, frame.ContentSize.Width, frame.ContentSize.Height));
    }

    private static async Task AwaitLoopAsync(Task? task)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private void Log(string message)
    {
        lock (_logLock)
        {
            Console.WriteLine($"[host] {DateTimeOffset.UtcNow:O} {message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _lifecycleGate.Dispose();
    }

    private readonly record struct RawFrame(IDirect3DSurface? Surface, int Width, int Height)
    {
        public static RawFrame Dummy() => new(null, 1280, 720);
    }
}

internal sealed class DummyNvencBridge
{
    public event Action<uint, byte[]>? OnEncodedAccessUnit;

    public void EncodeSurface(IDirect3DSurface? surface, int width, int height)
    {
        // Plumbing placeholder only.
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var fakeNal = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x09, 0x10 };
        OnEncodedAccessUnit?.Invoke(3000, fakeNal);
    }
}

internal static class CaptureDeviceFactory
{
    public static IDirect3DDevice? CreateD3DDevice()
    {
        return null;
    }
}

internal static class CaptureItemFactory
{
    public static GraphicsCaptureItem? TryCreatePrimaryDisplayItem()
    {
        return null;
    }
}
