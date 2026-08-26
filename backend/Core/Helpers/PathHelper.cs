namespace Core.Helpers
{
    public static class PathHelper
    {
        private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string _appFolderName = "StepinFlow_v2";
        public static string GetAppDataPath()
        {
            string appDataFolder = Path.Combine(_appDataPath, _appFolderName);

            if (!Directory.Exists(appDataFolder))
                Directory.CreateDirectory(appDataFolder);

            return appDataFolder;
        }

        public static string GetDatabaseDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Database");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string GetTempDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Temp");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string GetExportDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Export");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string GetExecutionRunPath(string flowName, DateTime startedOn)
        {
            string safeName = string.Concat(flowName.Split(Path.GetInvalidFileNameChars())).Trim();
            if (safeName.Length == 0)
                safeName = "Flow";

            string folder = $"{safeName} - {startedOn:yyyy-MM-dd HH.mm.ss}";
            string path = Path.Combine(GetExecutionHistoryDataPath(), folder);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string GetExecutionHistoryDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Execution History");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }
    }
}
