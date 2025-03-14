using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Drawing;
using System.Drawing.Imaging;
using Point = System.Windows.Point;
using Business.Services.Interfaces;
using OpenCvSharp.WpfExtensions;
using Business.Extensions;

namespace StepinFlow.ViewModels.Windows
{
    public partial class ScreenshotSelectionWindowVM : ObservableObject
    {
        private readonly ISystemService _systemService;


        public event CloseWindowEvent? CloseWindow;
        public delegate void CloseWindowEvent();

        public byte[]? ResultImage = null;


        private Stack<BitmapSource> _undoStack = new Stack<BitmapSource>();
        private Stack<BitmapSource> _redoStack = new Stack<BitmapSource>();
        private Point _startPoint;

        [ObservableProperty]
        private BitmapSource? _screenshot;
        [ObservableProperty]
        private double _imageActualWidth = 0;
        [ObservableProperty]
        private double _imageActualHeight = 0;

        [ObservableProperty]
        private double _rectangleTop = 0;
        [ObservableProperty]
        private double _rectangleLeft = 0;
        [ObservableProperty]
        private double _rectangleWidth = 0;
        [ObservableProperty]
        private double _rectangleHeight = 0;
        [ObservableProperty]
        private Visibility _rectangleVisibility = Visibility.Hidden;

        [ObservableProperty]
        private Visibility _importVisibility;

        public ScreenshotSelectionWindowVM(ISystemService systemService)
        {
            _systemService = systemService;
        }

        public void SetScreenshot(byte[]? screenshot = null)
        {
            if (screenshot != null)
            {
                Bitmap bitmapScreenshot;
                using (var ms = new MemoryStream(screenshot))
                    bitmapScreenshot = new Bitmap(ms);

                Screenshot = bitmapScreenshot.ToBitmapSource();
            }
            else
            {
                Model.Structs.Rectangle searchRectangle = _systemService.GetScreenSize();
                Bitmap? bitmapScreenshot = _systemService.TakeScreenShot(searchRectangle, "");
                if (bitmapScreenshot != null)
                    Screenshot = bitmapScreenshot.ToBitmapSource();
            }
        }

        [RelayCommand]
        private void OnCanvasMouseDown(MouseButtonEventArgs e)
        {
            Canvas? canvas = e.Source as Canvas;
            if (canvas == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            _startPoint = e.GetPosition(canvas);

            // Set initial position for the selection rectangle.
            RectangleLeft = _startPoint.X;
            RectangleTop = _startPoint.Y;
            RectangleWidth = 0;
            RectangleHeight = 0;
            RectangleVisibility = Visibility.Visible;
        }

        [RelayCommand]
        private void OnCanvasMouseMove(MouseEventArgs e)
        {
            Canvas? canvas = e.Source as Canvas;
            if (canvas == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point endPoint = e.GetPosition(canvas);

            // Calculate selection rectangle.
            RectangleLeft = Math.Min(endPoint.X, _startPoint.X);
            RectangleTop = Math.Min(endPoint.Y, _startPoint.Y);
            RectangleWidth = Math.Abs(endPoint.X - _startPoint.X);
            RectangleHeight = Math.Abs(endPoint.Y - _startPoint.Y);
        }

        [RelayCommand]
        private void OnCanvasMouseUp()
        {
            CroppedBitmap? croppedImage = CropImage();
            if (croppedImage == null || Screenshot == null)
                return;

            _undoStack.Push(Screenshot);
            _redoStack.Clear();
            Screenshot = croppedImage;
            RectangleVisibility = Visibility.Hidden;
        }



        [RelayCommand]
        private void OnButtonSaveClick()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "CroppedImage",
                DefaultExt = ".png",
                Filter = "PNG Image|*.png"
            };
            if (dlg.ShowDialog() == true)
            {
                using (FileStream stream = new FileStream(dlg.FileName, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(Screenshot));
                    encoder.Save(stream);
                }
            }

        }

        [RelayCommand]
        private void OnButtonImportClick()
        {
            if (Screenshot == null)
                return;

            using (MemoryStream stream = new MemoryStream())
            {
                // Convert BitmapSource to Bitmap.
                Bitmap resultBitmap = Screenshot.ToBitmap();
                resultBitmap.Save(stream, ImageFormat.Png);

                // Convert Bitmap to Byte[].
                ResultImage = stream.ToArray();
                CloseWindow?.Invoke();
            }
        }

        [RelayCommand]
        private void OnButtonRedoClick()
        {
            if (_redoStack.Count > 0 && Screenshot != null)
            {
                _undoStack.Push(Screenshot);
                Screenshot = _redoStack.Pop();
            }
        }
        [RelayCommand]

        private void OnButtonUndoClick()
        {
            if (_undoStack.Count > 0 && Screenshot != null)
            {
                _redoStack.Push(Screenshot);
                Screenshot = _undoStack.Pop();
            }
        }


        private CroppedBitmap? CropImage()
        {
            if (Screenshot == null)
                return null;

            // Convert selection area to original image coordinates.
            // (since the image is scaled to fit the window)
            double scaleX = Screenshot.PixelWidth / ImageActualWidth;
            double scaleY = Screenshot.PixelHeight / ImageActualHeight;

            int cropX = (int)(RectangleLeft * scaleX);
            int cropY = (int)(RectangleTop * scaleY);
            int cropWidth = (int)(RectangleWidth * scaleX);
            int cropHeight = (int)(RectangleHeight * scaleY);

            if (cropWidth <= 0 || cropHeight <= 0)
                return null;

            // Perform cropping
            CroppedBitmap croppedImage = new(Screenshot, new Int32Rect(cropX, cropY, cropWidth, cropHeight));
            return croppedImage;
        }
    }
}
