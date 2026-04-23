//
// Windows.Graphics.Capture helper — pure P/Invoke.
// Requires: Microsoft.Windows.SDK.Contracts (NuGet)
//
// Public surface:
//   byte[]? CaptureMonitorRaw(IntPtr hMonitor, out int w, out int h)  → raw BGRA
//   byte[]? CaptureWindowRaw (IntPtr hwnd,     out int w, out int h)  → raw BGRA
//   byte[]? CaptureMonitorCompressed(hMonitor, format, quality)    → JPEG/PNG
//   byte[]? CaptureWindowCompressed (hwnd,     format, quality)    → JPEG/PNG
//
// How it works 
//   WGC frame → IDirect3DDxgiInterfaceAccess → raw ID3D11Texture2D (GPU)
//   → D3D11 staging texture (CPU-readable)
//   → CopyResource (GPU copy, no DWM round-trip)
//   → Map/Unmap → row-by-row memcpy into byte[]
//

using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
namespace Business.Services.ScreenshotService
{
    public sealed class WindowsGraphicsCaptureService : IWindowsGraphicsCaptureService, IDisposable
    {


        // ================================================================
        // GUIDs  
        // ================================================================
        private static readonly Guid IID_GraphicsCaptureItem = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");


        // ================================================================
        // COM interface declarations 
        // ================================================================
        // IGraphicsCaptureItemInterop — the "secret" COM interface that lets us create GraphicsCaptureItem from an HMONITOR or HWND.
        [ComImport]                                              // Tells the .NET runtime: This is not a normal C# interface. It represents a native COM interface.
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]           // This is the exact GUID of the hidden IGraphicsCaptureItemInterop COM interface defined by Microsoft in the Windows SDK.
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]    // Tells .NET that this interface follows the classic IUnknown COM pattern
        //[ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
            IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
        }


        // ================================================================
        // Fields  
        // ================================================================
        private readonly IntPtr _devicePtr;             // raw ID3D11Device*
        private readonly IntPtr _contextPtr;            // raw ID3D11DeviceContext*
        private readonly IDirect3DDevice _wrtDevice;    // WinRT wrapper (for WGC frame pool)
        private bool _disposed;


        public WindowsGraphicsCaptureService()
        {
            IDirect3DDevice device = Direct3D11Helper.CreateDevice(out _devicePtr, out _contextPtr);
            _wrtDevice = device;
        }

        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Capture a single frame from the specified monitor.
        /// Capture monitor and compress to JPEG or PNG.
        /// This is what you call from ScreenshotService for the IPC transfer.
        /// Returns raw BGRA bytes (width × height × 4), or null on failure.
        /// </summary>
        public byte[]? CaptureMonitorRaw(IntPtr hMonitor, out int width, out int height)
        {
            width = 0; height = 0;
            GraphicsCaptureItem? item = CreateItemForMonitor(hMonitor);
            if (item is null) return null;
            return CaptureItem(item, out width, out height);
        }


        /// <summary>
        /// Capture a single frame from the specified window.
        /// Capture window and compress to JPEG or PNG.
        /// Returns raw BGRA bytes (width × height × 4), or null on failure.
        /// </summary>
        public byte[]? CaptureWindowRaw(IntPtr hwnd, out int width, out int height)
        {
            width = 0; height = 0;
            GraphicsCaptureItem? item = CreateItemForWindow(hwnd);
            if (item is null) return null;
            return CaptureItem(item, out width, out height);
        }



        // ================================================================
        // GraphicsCaptureItem factory
        // ================================================================

        private static GraphicsCaptureItem? CreateItemForMonitor(IntPtr hMonitor)
        {
            //if (hMonitor == IntPtr.Zero) return null;
            //try
            //{
            //    IGraphicsCaptureItemInterop interop = (IGraphicsCaptureItemInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));

            //    // Must be a local variable — 'ref' on a static field is a C# error.
            //    Guid iid = IID_GraphicsCaptureItem;
            //    IntPtr ptr = interop.CreateForMonitor(hMonitor, ref iid);

            //    var item = Marshal.GetObjectForIUnknown(ptr) as GraphicsCaptureItem;
            //    Marshal.Release(ptr); // we have a managed ref now; release the extra one
            //    return item;
            //}
            //catch (Exception ex)
            //{
            //    Console.Error.WriteLine($"[WGC] CreateItemForMonitor failed: {ex.Message}");
            //    return null;
            //}

            if (hMonitor == IntPtr.Zero) return null;

            try
            {
                //IGraphicsCaptureItemInterop interop = (IGraphicsCaptureItemInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
                Guid iid = IID_GraphicsCaptureItem;
                IntPtr ptr = interop.CreateForMonitor(hMonitor, ref iid);

                var item = Marshal.GetObjectForIUnknown(ptr) as GraphicsCaptureItem;
                Marshal.Release(ptr);
                return item;
            }


            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WGC] CreateItemForMonitor failed: {ex.Message}");
                return null;
            }

        }

        private static GraphicsCaptureItem? CreateItemForWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;
            try
            {
                //IGraphicsCaptureItemInterop interop = (IGraphicsCaptureItemInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
                //var interop = (IGraphicsCaptureItemInterop)WinRT.Interop.GetActivationFactory(typeof(GraphicsCaptureItem));
                Guid iid = IID_GraphicsCaptureItem;
                IntPtr ptr = interop.CreateForWindow(hwnd, ref iid);

                var item = Marshal.GetObjectForIUnknown(ptr) as GraphicsCaptureItem;
                Marshal.Release(ptr);
                return item;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WGC] CreateItemForWindow failed: {ex.Message}");
                return null;
            }
        }

        // ================================================================
        // Actual capture 
        // ================================================================



        private byte[]? CaptureItem(GraphicsCaptureItem item, out int width, out int height)
        {
            width = item.Size.Width;
            height = item.Size.Height;

            // CreateFreeThreaded is important: FrameArrived fires on a thread-pool thread,
            // not the UI thread, which avoids deadlocks in a console/service app like yours.
            using Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _wrtDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                numberOfBuffers: 1,
                item.Size);

            using GraphicsCaptureSession session = framePool.CreateCaptureSession(item);

            // Suppress the yellow capture border (requires Windows 11 22H2+, safe to ignore on older)
            try { session.IsBorderRequired = false; } catch { }
            // Hide the system cursor from the capture
            try { session.IsCursorCaptureEnabled = false; } catch { }

            Direct3D11CaptureFrame? capturedFrame = null;
            using ManualResetEventSlim frameReady = new ManualResetEventSlim(false);

            TypedEventHandler<Direct3D11CaptureFramePool, object> onFrame = (pool, _) =>
            {
                // TryGetNextFrame() dequeues from the pool — we own it and must Dispose it.
                capturedFrame = pool.TryGetNextFrame();
                frameReady.Set();
            };

            framePool.FrameArrived += onFrame;
            session.StartCapture();

            bool gotFrame = frameReady.Wait(millisecondsTimeout: 3000);
            framePool.FrameArrived -= onFrame;
            session.Dispose(); // stop capture immediately — we only need 1 frame

            if (!gotFrame || capturedFrame is null)
            {
                Console.Error.WriteLine("[WGC] Timeout waiting for frame.");
                return null;
            }

            using (capturedFrame)
            {
                return Direct3D11Helper.ReadStagingBytes(capturedFrame.Surface, _devicePtr, _contextPtr, width, height);
            }
        }


        // ================================================================
        // Compression 
        // ================================================================

        /// <summary>
        /// Convert raw BGRA bytes to JPEG or PNG without an extra heap copy.
        /// We pin the byte array and create a Bitmap view over it — no pixel copy needed.
        /// </summary>
        //private static byte[] BgraToCompressed(byte[] bgra, int width, int height, ScreenshotFormatEnum format, int jpegQuality)
        //{
        //    var pin = GCHandle.Alloc(bgra, GCHandleType.Pinned);
        //    try
        //    {
        //        // Bitmap backed directly by 'bgra' — no allocation
        //        using Bitmap bmp = new Bitmap(
        //            width, height,
        //            stride: width * 4,
        //            PixelFormat.Format32bppArgb,
        //            pin.AddrOfPinnedObject());

        //        int pixelCount = width * height;
        //        int initialCapacity = format == ScreenshotFormatEnum.JPEG
        //            ? pixelCount / 4   // JPEG: ~2 bits/pixel → /4 bytes is generous
        //            : pixelCount / 2;  // PNG:  harder to predict, raw/2 is a safe over-estimate

        //        using var ms = new MemoryStream(initialCapacity);



        //        if (format == ScreenshotFormatEnum.PNG)
        //        {
        //            bmp.Save(ms, ImageFormat.Png);
        //        }
        //        else
        //        {
        //            ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        //            var ep = new EncoderParameters(1)
        //            {
        //                Param = { [0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality) }
        //            };
        //            bmp.Save(ms, codec, ep);
        //        }

        //        return ms.ToArray();
        //    }
        //    finally
        //    {
        //        pin.Free();
        //    }
        //}



        // ================================================================
        // Dispose 
        // ================================================================

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_contextPtr != IntPtr.Zero) Marshal.Release(_contextPtr);
            if (_devicePtr != IntPtr.Zero) Marshal.Release(_devicePtr);
        }
    }
}