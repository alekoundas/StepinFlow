﻿
using Model.Business;
using Model.Enums;
using Model.Models;
using System.Drawing;

namespace Business.Interfaces
{
    public interface ISystemService
    {
        Bitmap? TakeScreenShot(Model.Structs.Rectangle rectangle, string filename = "Screenshot");

        Model.Structs.Rectangle GetWindowSize(string processName);
        Model.Structs.Rectangle GetScreenSize();
        bool MoveWindow(string processName, Model.Structs.Rectangle newWindowSize);
        void SetCursorPossition(Model.Structs.Point point);
        void CursorClick(MouseButtonsEnum mouseButtonEnum);
        Task ExportFlowsJSON(List<Flow> flows, string exportFilePath);
        List<Flow>? ImportFlowsJSON(string importFilePath);
        ImageSizeResult GetImageSize(byte[] imagePath);
        List<string> GetProcessWindowTitles();
        Task SaveImageToDisk(string filePath, byte[] image);
        void CopyImageToDisk(string sourceFilePath, string destinationFilePath);
        void CursorScroll(MouseScrollDirectionEnum scrollDirection, int steps);

    }
}
