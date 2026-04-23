
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;

namespace Business.Services.ScreenshotService
{
    public static class Direct3D11Helper
    {

        // ================================================================
        // Constants 
        // ================================================================
        private const int D3D_DRIVER_TYPE_HARDWARE = 1;
        private const uint D3D11_SDK_VERSION = 7;
        private const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;
        private const int D3D11_USAGE_STAGING = 3;
        private const int D3D11_CPU_ACCESS_READ = 0x20000;
        private const int D3D11_MAP_READ = 1;
        private const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;


        // ================================================================
        // vtable slot indices (these are fixed by the COM ABI — never change)
        // ================================================================
        private const int SLOT_QueryInterface = 0;
        private const int SLOT_ID3D11Device_CreateTexture2D = 5;
        private const int SLOT_ID3D11DeviceContext_CopyResource = 47;
        private const int SLOT_ID3D11DeviceContext_Map = 14;
        private const int SLOT_ID3D11DeviceContext_Unmap = 15;


        // ================================================================
        // GUIDs  
        // ================================================================

        private static readonly Guid IID_IDXGIDevice3 = new Guid("6007896c-3244-4afd-bf18-a6d3beda5023");

        private static readonly Guid IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");


        // ================================================================
        // P/Invoke 
        // ================================================================
        [DllImport("d3d11.dll", CallingConvention = CallingConvention.Winapi, PreserveSig = true)]
        private static extern int D3D11CreateDevice(
            IntPtr pAdapter,
            int DriverType,
            IntPtr Software,
            uint Flags,
            IntPtr pFeatureLevels,
            uint FeatureLevels,
            uint SDKVersion,
            out IntPtr ppDevice,
            out int pFeatureLevel,
            out IntPtr ppImmediateContext);

        // Converts a DXGI device COM pointer into the WinRT IDirect3DDevice wrapper
        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", CallingConvention = CallingConvention.Winapi, PreserveSig = true)]
        private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);


        // ================================================================
        // COM interface declarations 
        // ================================================================

        // IDirect3DDxgiInterfaceAccess — lets us unwrap a WinRT surface back to a raw DXGI/D3D11 COM pointer so we can use staging textures.
        [ComImport, Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirect3DDxgiInterfaceAccess
        {
            [PreserveSig]
            int GetInterface([In] ref Guid iid, out IntPtr p);
        }


        // ================================================================
        // Vtable delegate types
        // ================================================================
        // We call a few ID3D11Device / ID3D11DeviceContext methods that are
        // impractical to fully redeclare as [ComImport] interfaces, so we grab
        // them directly from the vtable.

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int QueryInterfaceDelegate(IntPtr pThis, ref Guid riid, out IntPtr ppvObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateTexture2DDelegate(IntPtr pThis, ref D3D11_TEXTURE2D_DESC pDesc, IntPtr pInitialData, out IntPtr ppTexture2D);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void CopyResourceDelegate(IntPtr pThis, IntPtr pDstResource, IntPtr pSrcResource);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int MapDelegate(IntPtr pThis, IntPtr pResource, uint Subresource, int MapType, uint MapFlags, out D3D11_MAPPED_SUBRESOURCE pMappedResource);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void UnmapDelegate(IntPtr pThis, IntPtr pResource, uint Subresource);

        // ================================================================
        // D3D structs 
        // ================================================================
        [StructLayout(LayoutKind.Sequential)]
        private struct D3D11_TEXTURE2D_DESC
        {
            public uint Width;
            public uint Height;
            public uint MipLevels;        // 1
            public uint ArraySize;        // 1
            public int Format;            // DXGI_FORMAT_B8G8R8A8_UNORM = 87
            public uint SampleDescCount;  // 1
            public uint SampleDescQuality;// 0
            public int Usage;             // D3D11_USAGE_STAGING = 3
            public uint BindFlags;        // 0 for staging
            public int CPUAccessFlags;    // D3D11_CPU_ACCESS_READ
            public uint MiscFlags;        // 0
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct D3D11_MAPPED_SUBRESOURCE
        {
            public IntPtr pData;
            public uint RowPitch;
            public uint DepthPitch;
        }



        public static IDirect3DDevice CreateDevice(out IntPtr devicePtr, out IntPtr contextPtr)
        {
            int hr = D3D11CreateDevice(
                    IntPtr.Zero,
                    D3D_DRIVER_TYPE_HARDWARE,
                    IntPtr.Zero,
                    D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                    IntPtr.Zero, 0,
                    D3D11_SDK_VERSION,
                    out devicePtr,      //raw ID3D11Device*
                    out _,
                    out contextPtr      // raw ID3D11DeviceContext*
                    );

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            // Wrap the raw D3D11 device in a WinRT IDirect3DDevice.
            // WGC's Direct3D11CaptureFramePool requires this wrapper, not the raw pointer.
            IDirect3DDevice winRTDevice = WrapDeviceAsWinRT(devicePtr);

            return winRTDevice;
        }

        /// <summary>
        /// Pipeline:
        ///   WinRT IDirect3DSurface
        ///     → IDirect3DDxgiInterfaceAccess.GetInterface → raw D3D11 texture (still on GPU)
        ///     → ID3D11Device.CreateTexture2D (USAGE_STAGING, CPU_ACCESS_READ) → staging texture (CPU-visible)
        ///     → ID3D11DeviceContext.CopyResource → GPU copy to staging
        ///     → ID3D11DeviceContext.Map → CPU pointer to staging memory
        ///     → row-by-row Marshal.Copy (handles RowPitch padding)
        ///     → ID3D11DeviceContext.Unmap → release CPU mapping
        ///     → Release staging texture
        ///
        /// </summary>
        public static byte[]? ReadStagingBytes(IDirect3DSurface surface, IntPtr devicePtr, IntPtr contextPtr, int width, int height)
        //private unsafe byte[]? ReadStagingBytes(IDirect3DSurface surface, int width, int height)
        {
            // 1. Unwrap WinRT surface to the raw D3D11 texture pointer.
            //    IDirect3DDxgiInterfaceAccess is the bridge between WinRT and COM/D3D.
            //IDirect3DDxgiInterfaceAccess access = (IDirect3DDxgiInterfaceAccess)surface;
            IntPtr surfacePtr = WinRT.MarshalInterface<IDirect3DSurface>.FromManaged(surface);
            IDirect3DDxgiInterfaceAccess access = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(surfacePtr);


            //Guid texGuid = IID_IDXGISurface; // The frame texture implements IDXGISurface
            //int hr = access.GetInterface(ref texGuid, out IntPtr frameSurfacePtr);
            //if (hr < 0 || frameSurfacePtr == IntPtr.Zero)
            //{
            //    Console.Error.WriteLine($"[WGC] GetInterface(IDXGISurface) failed: 0x{hr:X8}");
            //    return null;
            //}

            //// QI IDXGISurface → ID3D11Resource 
            //QueryInterfaceDelegate fnQI = GetVtableFunc<QueryInterfaceDelegate>(frameSurfacePtr, SLOT_QueryInterface);
            //Guid resGuid = IID_ID3D11Resource;
            //hr = fnQI(frameSurfacePtr, ref resGuid, out IntPtr frameResourcePtr);
            //Marshal.Release(frameSurfacePtr); // done with the IDXGISurface ptr
            //if (hr < 0 || frameResourcePtr == IntPtr.Zero)
            //{
            //    Console.Error.WriteLine($"[WGC] QI ID3D11Resource failed: 0x{hr:X8}");
            //    return null;
            //}

            // Ask for ID3D11Texture2D directly — this IS an ID3D11Resource, no QI needed
            Guid texGuid = IID_ID3D11Texture2D;
            int hr = access.GetInterface(ref texGuid, out IntPtr frameResourcePtr);
            if (hr < 0 || frameResourcePtr == IntPtr.Zero)
            {
                Console.Error.WriteLine($"[WGC] GetInterface(ID3D11Texture2D) failed: 0x{hr:X8}");
                return null;
            }



            // 2. Create a CPU-readable staging texture with identical dimensions/format.
            //    USAGE_STAGING + CPU_ACCESS_READ = the texture lives in CPU-visible memory
            //    after we copy into it.
            D3D11_TEXTURE2D_DESC stagingDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDescCount = 1,
                SampleDescQuality = 0,
                Usage = D3D11_USAGE_STAGING,
                BindFlags = 0,
                CPUAccessFlags = D3D11_CPU_ACCESS_READ,
                MiscFlags = 0
            };

            CreateTexture2DDelegate fnCreateTexture2D = GetVtableFunc<CreateTexture2DDelegate>(devicePtr, SLOT_ID3D11Device_CreateTexture2D);

            hr = fnCreateTexture2D(devicePtr, ref stagingDesc, IntPtr.Zero, out IntPtr stagingPtr);
            if (hr < 0 || stagingPtr == IntPtr.Zero)
            {
                Marshal.Release(frameResourcePtr);
                Console.Error.WriteLine($"[WGC] CreateTexture2D(staging) failed: 0x{hr:X8}");
                return null;
            }

            try
            {
                // 3. GPU copy: frame texture → staging texture.
                //    This is asynchronous on the GPU but CopyResource() on the immediate
                //    context synchronizes before Map().
                var fnCopyResource = GetVtableFunc<CopyResourceDelegate>(contextPtr, SLOT_ID3D11DeviceContext_CopyResource);
                fnCopyResource(contextPtr, stagingPtr, frameResourcePtr);

                // 4. Map: get a CPU pointer to the staging texture's memory.
                //    D3D11_MAP_READ stalls until the GPU copy above is complete.
                var fnMap = GetVtableFunc<MapDelegate>(contextPtr, SLOT_ID3D11DeviceContext_Map);
                hr = fnMap(contextPtr, stagingPtr, 0, D3D11_MAP_READ, 0, out var mapped);
                if (hr < 0)
                {
                    Console.Error.WriteLine($"[WGC] Map failed: 0x{hr:X8}");
                    return null;
                }

                try
                {
                    // 5. Copy row by row.
                    //    RowPitch may be larger than (width * 4) due to GPU alignment padding.
                    //    We must skip padding bytes at the end of each row.
                    int destStride = width * 4; // bytes per row we actually want
                    byte[] result = new byte[destStride * height];

                    for (int row = 0; row < height; row++)
                    {
                        IntPtr src = IntPtr.Add(mapped.pData, (int)(row * mapped.RowPitch));
                        Marshal.Copy(src, result, row * destStride, destStride);
                    }

                    return result; // BGRA, width × height × 4 bytes
                }
                finally
                {
                    // 6. Unmap — must always be called after Map, even on error.
                    var fnUnmap = GetVtableFunc<UnmapDelegate>(contextPtr, SLOT_ID3D11DeviceContext_Unmap);
                    fnUnmap(contextPtr, stagingPtr, 0);
                }
            }
            finally
            {
                Marshal.Release(stagingPtr);
                Marshal.Release(frameResourcePtr);
            }
        }


        // ================================================================
        // Private methods 
        // ================================================================

        /// <summary>
        /// WGC's frame pool requires a WinRT IDirect3DDevice, not a raw ID3D11Device*.
        /// CreateDirect3D11DeviceFromDXGIDevice() (exported from d3d11.dll) converts
        /// a IDXGIDevice (QI'd from ID3D11Device) into the WinRT wrapper.
        /// </summary>
        private static IDirect3DDevice WrapDeviceAsWinRT(IntPtr d3dDevicePtr)
        {
            // QI: ID3D11Device → IDXGIDevice3
            Guid dxgiG = IID_IDXGIDevice3;
            QueryInterfaceDelegate queryInterfaceFunc = GetVtableFunc<QueryInterfaceDelegate>(d3dDevicePtr, SLOT_QueryInterface);
            int hr = queryInterfaceFunc(d3dDevicePtr, ref dxgiG, out IntPtr dxgiPtr);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);

            try
            {
                // Convert IDXGIDevice → WinRT IDirect3DDevice
                hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiPtr, out IntPtr wrtPtr);
                if (hr < 0) Marshal.ThrowExceptionForHR(hr);

                try
                {
                    //IDirect3DDevice wrtDevice = (IDirect3DDevice)Marshal.GetObjectForIUnknown(wrtPtr);
                    IDirect3DDevice wrtDevice = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(wrtPtr);
                    return wrtDevice;
                }
                finally
                {
                    Marshal.Release(wrtPtr); // managed object holds a ref now
                }
            }
            finally
            {
                Marshal.Release(dxgiPtr);
            }
        }

        // ─── Vtable helper ────────────────────────────────────────────────────

        private static T GetVtableFunc<T>(IntPtr comPtr, int slotIndex) where T : Delegate
        {
            IntPtr vtable = Marshal.ReadIntPtr(comPtr);
            IntPtr funcPtr = Marshal.ReadIntPtr(vtable, slotIndex * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
    }
}