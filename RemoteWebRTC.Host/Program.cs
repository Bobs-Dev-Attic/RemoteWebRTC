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

        var streamer = new WgcToWebRtcStreamer();
        await streamer.StartAsync();

        Console.WriteLine("Press ENTER to stop.");
        Console.ReadLine();

        await streamer.StopAsync();
    }
}

internal sealed class WgcToWebRtcStreamer : IDisposable
{
    private readonly RTCPeerConnection _peerConnection;
    private readonly MediaStreamTrack _videoTrack;
    private readonly DummyNvencBridge _nvencBridge;

    private GraphicsCaptureSession? _captureSession;
    private Direct3D11CaptureFramePool? _framePool;
    private Timer? _dummyTimer;

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

        _nvencBridge.OnEncodedAccessUnit += (durationRtp, payload) =>
        {
            // The DummyNvencBridge models the real flow where a D3D texture pointer
            // is consumed by NVENC and emits encoded H264 NAL units.
            _peerConnection.SendVideo(durationRtp, payload);
        };

    }

    public Task StartAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            StartDummyFrameLoop("Running on non-Windows runtime; using dummy encoded samples.");
            return Task.CompletedTask;
        }

        if (!GraphicsCaptureSession.IsSupported())
        {
            StartDummyFrameLoop("Windows.Graphics.Capture not supported in this runtime context.");
            return Task.CompletedTask;
        }

        var item = CaptureItemFactory.TryCreatePrimaryDisplayItem();
        var device = CaptureDeviceFactory.CreateD3DDevice();

        if (item is null || device is null)
        {
            StartDummyFrameLoop("Capture item or D3D device is not available yet; using dummy encoded samples.");
            return Task.CompletedTask;
        }

        _framePool = Direct3D11CaptureFramePool.Create(
            device,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            item.Size);

        _captureSession = _framePool.CreateCaptureSession(item);
        _framePool.FrameArrived += OnFrameArrived;
        _captureSession.StartCapture();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_framePool is not null)
        {
            _framePool.FrameArrived -= OnFrameArrived;
        }

        _dummyTimer?.Dispose();
        _captureSession?.Dispose();
        _framePool?.Dispose();
        _peerConnection.Close("Host stopped");

        return Task.CompletedTask;
    }


    private void StartDummyFrameLoop(string reason)
    {
        Console.WriteLine($"[host] {reason}");
        _dummyTimer = new Timer(_ => _nvencBridge.EmitTestFrame(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(33));
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame is null)
        {
            return;
        }

        // Zero-copy handoff placeholder: Direct3DSurface stays GPU-backed.
        IDirect3DSurface surface = frame.Surface;

        _nvencBridge.EncodeSurface(surface, frame.ContentSize.Width, frame.ContentSize.Height);
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}

internal sealed class DummyNvencBridge
{
    private int _targetBitrateBps = 1_000_000;

    public event Action<uint, byte[]>? OnEncodedAccessUnit;

    public void EncodeSurface(IDirect3DSurface surface, int width, int height)
    {
        // Plumbing placeholder only.
        // In production this method should:
        // 1) obtain the underlying ID3D11Texture2D pointer from `surface`,
        // 2) feed it directly into NVENC with low-latency preset,
        // 3) emit H264 NAL units.
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // A tiny fake keyframe-like payload to keep this MVP wiring testable.
        // Replace with real encoder output as soon as NVENC integration is ready.
        var fakeNal = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x09, 0x10 };
        OnEncodedAccessUnit?.Invoke(3000, fakeNal);
    }

    public void IncreaseBitrate() => _targetBitrateBps = Math.Min(15_000_000, _targetBitrateBps + 250_000);

    public void DecreaseBitrate() => _targetBitrateBps = Math.Max(1_000_000, _targetBitrateBps - 250_000);

    public void EmitTestFrame()
    {
        var fakeNal = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x09, 0x10 };
        OnEncodedAccessUnit?.Invoke(3000, fakeNal);
    }
}

internal static class CaptureDeviceFactory
{
    public static IDirect3DDevice? CreateD3DDevice()
    {
        // Placeholder for SharpDX DXGI -> WinRT IDirect3DDevice interop.
        return null;
    }
}

internal static class CaptureItemFactory
{
    public static GraphicsCaptureItem? TryCreatePrimaryDisplayItem()
    {
        // TODO: Implement GraphicsCaptureItem creation for monitor/window selection.
        // For unattended host flow, use IGraphicsCaptureItemInterop for primary display handle.
        return null;
    }
}
