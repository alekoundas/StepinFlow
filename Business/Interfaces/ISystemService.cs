﻿
using Model.Business;
using Model.Enums;
using Model.Models;
using System.Drawing;

namespace Business.Interfaces
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
        Task SaveImageToDisk(string filePath, byte[] image);
        void CopyImageToDisk(string sourceFilePath, string destinationFilePath);
        Bitmap? TakeScreenShot(Model.Structs.Rectangle rectangle, string filename = "Screenshot");
        byte[]? TakeScreenShot(Model.Structs.Rectangle rectangle);

        // Window.
        bool MoveWindow(string processName, Model.Structs.Rectangle newWindowSize);

        // Cursor.
        void SetCursorPossition(Model.Structs.Point point);
        void CursorClick(MouseButtonsEnum mouseButtonEnum);
        void CursorScroll(MouseScrollDirectionEnum scrollDirection, int steps);

    }
}
