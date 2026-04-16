using Core.Enums;
using Core.Models.Business;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace Business.Helpers
{
    public static class WGCHelper
    {
        private static readonly Guid _graphicsCaptureItemGuid = new Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");

        [ComImport]                                              // Tells the .NET runtime: This is not a normal C# interface. It represents a native COM interface.
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]           // This is the exact GUID of the hidden IGraphicsCaptureItemInterop COM interface defined by Microsoft in the Windows SDK.
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]    // Tells .NET that this interface follows the classic IUnknown COM pattern
        public interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
            IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
        }


        private static GraphicsCaptureItem? CreateForMonitor(IntPtr hMonitor)
        {
            if (hMonitor == IntPtr.Zero) return null;

            try
            {
                IGraphicsCaptureItemInterop interop = (IGraphicsCaptureItemInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                Guid iid = _graphicsCaptureItemGuid; //Use a local variable for ref
                IntPtr ptr = interop.CreateForMonitor(hMonitor, ref iid);

                GraphicsCaptureItem? item = Marshal.GetObjectForIUnknown(ptr) as GraphicsCaptureItem;
                Marshal.Release(ptr);   // Release the COM pointer
                return item;
            }
            catch
            {
                return null;
            }
        }

        private static GraphicsCaptureItem? CreateForWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;

            try
            {
                IGraphicsCaptureItemInterop interop = (IGraphicsCaptureItemInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                Guid iid = _graphicsCaptureItemGuid;
                IntPtr ptr = interop.CreateForWindow(hwnd, ref iid);

                GraphicsCaptureItem? item = Marshal.GetObjectForIUnknown(ptr) as GraphicsCaptureItem;
                Marshal.Release(ptr);   // Release the COM pointer

                return item;
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? CaptureFrameToBytes(Direct3D11CaptureFrame frame)
        {
            if (frame?.Surface == null) return null;

            try
            {
                // Direct3D mapping 
                using var surface = frame.Surface;
                Direct3DSurfaceDescription desc = surface.Description;

                // TODO: True Direct3D mapping requires SharpDX or heavy P/Invoke.

                // Safer & simpler path using SoftwareBitmap (good enough for now)
                SoftwareBitmap softwareBitmap = SoftwareBitmap.CreateCopyFromSurfaceAsync(surface)
                    .AsTask().Result;

                if (softwareBitmap == null) return null;

                int bufferSize = (int)(softwareBitmap.PixelWidth * softwareBitmap.PixelHeight * 4); // BGRA
                byte[] bytes = new byte[bufferSize];

                softwareBitmap.CopyToBuffer(bytes.AsBuffer());   // Requires System.Runtime.InteropServices.WindowsRuntime

                return bytes;   // BGRA format
            }
            catch
            {
                return null;
            }
        }

        private static byte[] CaptureWithWgc(GraphicsCaptureItem captureItem, ScreenshotFormatEnum format, int jpegQuality)
        {
            IDirect3DDevice d3dDevice = CreateForMonitor
            using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(_d3dDevice!, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, captureItem.Size);
            using var session = framePool.CreateCaptureSession(captureItem);

            var tcs = new TaskCompletionSource<Direct3D11CaptureFrame?>();
            TypedEventHandler<Direct3D11CaptureFramePool, object> handler = (pool, _) =>
            {
                tcs.TrySetResult(pool.TryGetNextFrame());
            };

            framePool.FrameArrived += handler;
            session.StartCapture();

            var frame = tcs.Task.Wait(2000) ? tcs.Task.Result : null;
            framePool.FrameArrived -= handler;

            if (frame == null) return Array.Empty<byte>();

            using var bmp = GraphicsCaptureHelper.ConvertToBitmap(frame);
            return bmp != null ? Compress(bmp, format, jpegQuality) : Array.Empty<byte>();
        }

        public static byte[] CaptureMonitor(string monitorUniqueId)
        {
            MonitorInfo? monitorInfo = ScreenHelper.GetAllMonitors().FirstOrDefault(x => x.UniqueId == monitorUniqueId);
            IntPtr hMonitor = ScreenHelper.GetMonitorHandle(monitorUniqueId);

            GraphicsCaptureItem? captureItem = CreateForMonitor(hMonitor);
            byte[] result = CaptureFrameToBytes(captureItem.);
        }

        private static async Task<byte[]?> CaptureToBytesAsync(GraphicsCaptureItem captureItem)
        {
            if (captureItem == null) return null;

            try
            {
                using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    // You need a valid IDirect3DDevice here. For now we'll use a simple version.
                    // In practice you need to create one (see note below)
                    null!, // Placeholder - replace with your D3D device
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    captureItem.Size);

                using var session = framePool.CreateCaptureSession(captureItem);

                var tcs = new TaskCompletionSource<Direct3D11CaptureFrame?>();

                TypedEventHandler<Direct3D11CaptureFramePool, object> handler = (pool, _) =>
                {
                    try
                    {
                        var frame = pool.TryGetNextFrame();
                        if (frame != null)
                            tcs.TrySetResult(frame);
                    }
                    catch { }
                };

                framePool.FrameArrived += handler;
                session.StartCapture();

                // Wait for first frame with timeout
                var frame = await Task.WhenAny(tcs.Task, Task.Delay(3000)).Result == tcs.Task
                    ? await tcs.Task
                    : null;

                framePool.FrameArrived -= handler;

                if (frame == null) return null;

                return await FrameToBytesAsync(frame);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WGC Capture failed: {ex.Message}");
                return null;
            }
        }
    }
}
