using Business.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using Model.Models;
using Newtonsoft.Json;
using System.Windows.Forms;
using AutoMapper;
using Model.Enums;
using Model.Business;
using Path = System.IO.Path;
using System.Drawing.Imaging;
using Model.Structs;
using Business.Services.Interfaces;

namespace Business.Services
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class SystemService : ISystemService
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Model.Structs.Point lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetWindowRect(int hWnd, out Model.Structs.Rectangle lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(int hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, InputStruct[] inputs, int sizeOfInputStructure);



        public void CursorScroll(MouseScrollDirectionEnum scrollDirection, int steps)
        {
            int wheelOrientation = 0;
            int direction = 120;
            switch (scrollDirection)
            {
                case MouseScrollDirectionEnum.UP:
                    wheelOrientation = 0x0800;
                    break;
                case MouseScrollDirectionEnum.DOWN:
                    wheelOrientation = 0x0800;
                    direction *= -1;
                    break;
                case MouseScrollDirectionEnum.LEFT:
                    wheelOrientation = 0x01000;
                    break;
                case MouseScrollDirectionEnum.RIGT:
                    wheelOrientation = 0x01000;
                    direction *= -1;
                    break;
                default:
                    break;
            }

            for (int i = 0; i < steps; i++)
            {
                Thread.Sleep(50);
                mouse_event(wheelOrientation, 0, 0, direction, 0);
            }
        }


        public void CursorClick(MouseButtonsEnum mouseButtonEnum)
        {
            int x = System.Windows.Forms.Cursor.Position.X;
            int y = System.Windows.Forms.Cursor.Position.Y;

            Thread.Sleep(100);
            if (mouseButtonEnum == MouseButtonsEnum.RIGHT_BUTTON)
            {
                mouse_event(0x0008, x, y, 0, 0);
                Thread.Sleep(50);
                mouse_event(0x0010, x, y, 0, 0);
            }
            else if (mouseButtonEnum == MouseButtonsEnum.LEFT_BUTTON)
            {
                mouse_event(0x0002, x, y, 0, 0);
                Thread.Sleep(50);
                mouse_event(0x0004, x, y, 0, 0);
            }
        }

        public List<string> GetProcessWindowTitles()
        {
            return Process
                .GetProcesses()
                .Where(process => !String.IsNullOrEmpty(process.MainWindowTitle))
                .Select(x => x.MainWindowTitle)
                .ToList();
        }





        // Cache codec info as static fields (shared across all instances)
        private static readonly ImageCodecInfo JpegCodec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.MimeType == "image/jpeg");
        private static readonly ImageCodecInfo PngCodec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.MimeType == "image/png");

        // Cache EncoderParameter for common quality values
        private static readonly Dictionary<long, EncoderParameters> QualityParamsCache = new();

       
        public async Task<string> SaveImageToDisk(string filePath, byte[] image, double quality = 100.0)
        {
            // Validate quality parameter
            if (quality < 0 || quality > 100)
            {
                throw new ArgumentException("Quality must be between 0 and 100.", nameof(quality));
            }

            // If quality is 100, save original bytes directly
            if (quality == 100.0)
            {
                await File.WriteAllBytesAsync(filePath, image);
                return filePath;
            }


            // Use Span-based MemoryStream constructor to avoid copying
            using (var memoryStream = new MemoryStream(image, writable: false))
            using (var originalImage = Image.FromStream(memoryStream, useEmbeddedColorManagement: false))
            {
                // Get or create EncoderParameters
                long qualityLong = (long)quality;
                if (!QualityParamsCache.TryGetValue(qualityLong, out var encoderParameters))
                {
                    encoderParameters = new EncoderParameters(1)
                    {
                        Param = { [0] = new EncoderParameter(Encoder.Quality, qualityLong) }
                    };

                    // Thread-safe cache update
                    lock (QualityParamsCache)
                    {
                        if (!QualityParamsCache.ContainsKey(qualityLong))
                            QualityParamsCache[qualityLong] = encoderParameters;
                    }
                }

                // Use single MemoryStream for conversion
                using (var conversionStream = new MemoryStream())
                {
                    originalImage.Save(conversionStream, JpegCodec, encoderParameters);
                    conversionStream.Position = 0;

                    using (var jpegImage = Image.FromStream(conversionStream, useEmbeddedColorManagement: false))
                    using (var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                    {
                        jpegImage.Save(outputStream, PngCodec, null);
                        await outputStream.FlushAsync();
                    }
                }
            }

            return filePath;
        }

        public void CopyImageToDisk(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                // Ensure the destination directory exists
                string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationDirectory) && destinationDirectory != null)
                    Directory.CreateDirectory(destinationDirectory);

                // Copy the file to the new location with the new name
                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public byte[]? TakeScreenShot(Model.Structs.Rectangle rectangle)
        {
            int width = rectangle.Right - rectangle.Left;
            int height = rectangle.Bottom - rectangle.Top;

            if (width <= 0 || height <= 0)
                return null;

            using (Bitmap bmp = new Bitmap(Math.Abs(width), height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.CopyFromScreen(rectangle.Left, rectangle.Top, 0, 0, new System.Drawing.Size(width, height));
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }


        public Bitmap? TakeScreenShot(Model.Structs.Rectangle rectangle, string filename = "Screenshot")
        {
            string filePath = Path.Combine(PathHelper.GetAppDataPath(), filename + ".png");

            int width = rectangle.Right - rectangle.Left;
            int height = rectangle.Bottom - rectangle.Top;

            if (width <= 0 || height <= 0)
                return null;

            // Delete the existing file if it exists
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                    return null; // If we can't delete, we can't proceed
                }
            }

            // Take screenshot
            using (Bitmap bmp = new Bitmap(Math.Abs(width), height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.CopyFromScreen(rectangle.Left, rectangle.Top, 0, 0, new System.Drawing.Size(width, height));
                }

                // Save the image, ensuring it's fully written
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    bmp.Save(fileStream, ImageFormat.Png);
                    fileStream.Flush(); // Ensure all data is written to disk
                }
            }

            // Wait until the file is fully written
            FileInfo fileInfo = new FileInfo(filePath);
            while (fileInfo.Length == 0)
            {
                Thread.Sleep(10); // Wait a bit before checking again
                fileInfo.Refresh();
            }

            // Load the image from the file (returning a new Bitmap to avoid file locking issues)
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return new Bitmap(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading screenshot: {ex.Message}");
                return null;
            }
        }

        public Model.Structs.Rectangle? GetWindowSize(string processName)
        {
            var applicationProcess = Process.GetProcessesByName(processName).FirstOrDefault();
            if (applicationProcess != null)
            {
                var applicationHandle = applicationProcess.MainWindowHandle.ToInt32();
                GetWindowRect(applicationHandle, out Model.Structs.Rectangle windowRectangle);
                return windowRectangle;
            }

            return null;
        }

        public bool MoveWindow(string processName, Model.Structs.Rectangle newWindowSize)
        {
            var applicationProcess = Process.GetProcessesByName(processName).FirstOrDefault();
            if (applicationProcess == null)
                return false;

            var applicationHandle = applicationProcess.MainWindowHandle.ToInt32();

            int x = newWindowSize.Left;
            int y = newWindowSize.Top;
            int width = newWindowSize.Right - newWindowSize.Left;
            int height = newWindowSize.Bottom - newWindowSize.Top;

            bool result = MoveWindow(applicationHandle, x, y, width, height, true);
            return result;
        }


        public Model.Structs.Rectangle GetScreenSize()
        {
            Model.Structs.Rectangle windowRectangle = new Model.Structs.Rectangle();

            windowRectangle.Top = SystemInformation.VirtualScreen.Top;
            windowRectangle.Bottom = SystemInformation.VirtualScreen.Bottom;
            windowRectangle.Left = SystemInformation.VirtualScreen.Left;
            windowRectangle.Right = SystemInformation.VirtualScreen.Right;

            return windowRectangle;
        }

        public List<SystemMonitor> GetAllSystemMonitors()
        {
            List<SystemMonitor> systemMonitors = new List<SystemMonitor>();

            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                SystemMonitor systemMonitor = new SystemMonitor
                {
                    DeviceName = screen.DeviceName,
                    Top = screen.Bounds.Top,
                    Bottom = screen.Bounds.Bottom,
                    Left = screen.Bounds.Left,
                    Right = screen.Bounds.Right
                };

                systemMonitors.Add(systemMonitor);
            }

            return systemMonitors;
        }

        public Model.Structs.Rectangle? GetMonitorArea(string deviceName)
        {
            System.Windows.Forms.Screen? screen = System.Windows.Forms.Screen.AllScreens.Where(x => x.DeviceName == deviceName).FirstOrDefault();

            if (screen == null)
                return null;

            Model.Structs.Rectangle rectangle = new Model.Structs.Rectangle
            {
                Top = screen.Bounds.Top,
                Bottom = screen.Bounds.Bottom,
                Left = screen.Bounds.Left,
                Right = screen.Bounds.Right
            };

            return rectangle;
        }

        public ImageSizeResult GetImageSize(byte[] imageArray)
        {
            ImageSizeResult imageSizeResult = new ImageSizeResult();
            Bitmap image;
            using (var ms = new MemoryStream(imageArray))
            {
                image = new Bitmap(ms);
                imageSizeResult.Height = image.Height;
                imageSizeResult.Width = image.Width;
            }

            return imageSizeResult;
        }

        public void SetCursorPossition(Model.Structs.Point point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public Model.Structs.Point GetCursorPossition()
        {
             GetCursorPos(out Model.Structs.Point point);
            return point;
        }

        public async Task ExportFlowsJSON(List<Flow> flows, string exportFilePath)
        {
            var mapper = new MapperConfiguration(x =>
            {
                x.CreateMap<Flow, FlowDto>();
                x.CreateMap<FlowStep, FlowStepDto>();
                x.CreateMap<FlowParameter, FlowParameterDto>();
            }).CreateMapper();

            List<FlowDto> flowsDto = mapper.Map<List<FlowDto>>(flows);
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            string json = JsonConvert.SerializeObject(flowsDto, jsonSerializerSettings);
            await File.WriteAllTextAsync(exportFilePath, json);
        }

        public List<Flow>? ImportFlowsJSON(string importFilePath)
        {
            string flowsJSON = File.ReadAllText(importFilePath);
            if (flowsJSON != null)
            {
                List<Flow>? JsonFlows = JsonConvert.DeserializeObject<List<Flow>>(flowsJSON);
                if (JsonFlows != null && JsonFlows.Count > 0)
                    return JsonFlows;
            }

            return null;
        }

    
    }
}
