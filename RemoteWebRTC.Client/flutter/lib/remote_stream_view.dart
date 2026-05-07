import 'package:flutter/material.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';

enum RemoteStreamStatus {
  loading,
  live,
  noSignal,
  permissionDenied,
  reconnecting,
  error,
}

class RemoteStreamView extends StatefulWidget {
  const RemoteStreamView({
    super.key,
    required this.stream,
    this.status = RemoteStreamStatus.live,
    this.errorMessage,
    this.onRetry,
  });

  final MediaStream? stream;
  final RemoteStreamStatus status;
  final String? errorMessage;
  final VoidCallback? onRetry;

  @override
  State<RemoteStreamView> createState() => _RemoteStreamViewState();
}

class _RemoteStreamViewState extends State<RemoteStreamView> {
  final RTCVideoRenderer _renderer = RTCVideoRenderer();
  bool _rendererReady = false;

  @override
  void initState() {
    super.initState();
    _initializeRenderer();
  }

  Future<void> _initializeRenderer() async {
    await _renderer.initialize();
    _renderer.srcObject = widget.stream;
    if (mounted) {
      setState(() {
        _rendererReady = true;
      });
    }
  }

  @override
  void didUpdateWidget(covariant RemoteStreamView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.stream?.id != widget.stream?.id) {
      _renderer.srcObject = widget.stream;
    }
  }

  @override
  void dispose() {
    _renderer.srcObject = null;
    _renderer.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AspectRatio(
      aspectRatio: 16 / 9,
      child: DecoratedBox(
        decoration: const BoxDecoration(color: Colors.black),
        child: Stack(
          fit: StackFit.expand,
          children: [
            if (_rendererReady && widget.stream != null)
              RTCVideoView(
                _renderer,
                objectFit: RTCVideoViewObjectFit.RTCVideoViewObjectFitContain,
              ),
            _buildOverlay(context),
          ],
        ),
      ),
    );
  }

  Widget _buildOverlay(BuildContext context) {
    final status = _rendererReady ? widget.status : RemoteStreamStatus.loading;

    switch (status) {
      case RemoteStreamStatus.live:
        return const SizedBox.shrink();
      case RemoteStreamStatus.loading:
        return _overlayCard(
          context,
          icon: Icons.hourglass_top_rounded,
          title: 'Loading stream',
          subtitle: 'Initializing remote renderer…',
          showProgress: true,
        );
      case RemoteStreamStatus.reconnecting:
        return _overlayCard(
          context,
          icon: Icons.refresh_rounded,
          title: 'Reconnecting',
          subtitle: 'Trying to restore the session…',
          showProgress: true,
        );
      case RemoteStreamStatus.noSignal:
        return _overlayCard(
          context,
          icon: Icons.wifi_off_rounded,
          title: 'No signal',
          subtitle: 'The host is reachable, but no video is flowing.',
          actionLabel: 'Retry',
        );
      case RemoteStreamStatus.permissionDenied:
        return _overlayCard(
          context,
          icon: Icons.lock_outline_rounded,
          title: 'Permission denied',
          subtitle: 'You do not have access to this stream.',
        );
      case RemoteStreamStatus.error:
        return _overlayCard(
          context,
          icon: Icons.error_outline_rounded,
          title: 'Stream error',
          subtitle: widget.errorMessage ?? 'An unexpected error occurred.',
          actionLabel: 'Retry',
        );
    }
  }

  Widget _overlayCard(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String subtitle,
    String? actionLabel,
    bool showProgress = false,
  }) {
    final textTheme = Theme.of(context).textTheme;

    return Container(
      color: Colors.black54,
      alignment: Alignment.center,
      padding: const EdgeInsets.all(20),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 360),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: Colors.black87,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: Colors.white24),
          ),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon, color: Colors.white, size: 28),
                const SizedBox(height: 10),
                Text(
                  title,
                  textAlign: TextAlign.center,
                  style: textTheme.titleMedium?.copyWith(color: Colors.white),
                ),
                const SizedBox(height: 6),
                Text(
                  subtitle,
                  textAlign: TextAlign.center,
                  style: textTheme.bodyMedium?.copyWith(color: Colors.white70),
                ),
                if (showProgress) ...[
                  const SizedBox(height: 12),
                  const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                ],
                if (actionLabel != null && widget.onRetry != null) ...[
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: widget.onRetry,
                    child: Text(actionLabel),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
