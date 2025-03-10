using Model.Business;
using Model.Enums;
using Model.Models;
using System.Drawing;

namespace Business.Services.Interfaces
{
    public interface ISystemService
    {

        // Screen.
        Model.Structs.Rectangle? GetWindowSize(string processName);
        Model.Structs.Rectangle GetScreenSize();
        List<SystemMonitor> GetAllSystemMonitors();
        Model.Structs.Rectangle? GetMonitorArea(string deviceName);


        List<string> GetProcessWindowTitles();


        // JSON.
        Task ExportFlowsJSON(List<Flow> flows, string exportFilePath);
        List<Flow>? ImportFlowsJSON(string importFilePath);

        // Image.
        ImageSizeResult GetImageSize(byte[] imagePath);
        Task<string> SaveImageToDisk(string filePath, byte[] image, double quality = 100.0);
        void CopyImageToDisk(string sourceFilePath, string destinationFilePath);
        Bitmap? TakeScreenShot(Model.Structs.Rectangle rectangle, string filename = "Screenshot");
        byte[]? TakeScreenShot(Model.Structs.Rectangle rectangle);

        // Window.
        bool MoveWindow(string processName, Model.Structs.Rectangle newWindowSize);

        // Cursor.
        void SetCursorPossition(Model.Structs.Point point);
        Model.Structs.Point GetCursorPossition();

        void CursorClick(MouseButtonsEnum mouseButtonEnum);
        void CursorScroll(MouseScrollDirectionEnum scrollDirection, int steps);

    }
}
