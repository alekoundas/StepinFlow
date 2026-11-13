namespace Core.Helpers
{
    public static class PathHelper
    {
        private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string _appFolderName = "StepinFlow";


        /// <summary> Get the root app folder path. Creates it if missing. </summary>
        public static string GetAppDataPath()
        {
            string appDataFolder = Path.Combine(_appDataPath, _appFolderName);

            if (!Directory.Exists(appDataFolder))
                Directory.CreateDirectory(appDataFolder);

            return appDataFolder;
        }

        /// <summary> Get ".../Database" path. Creates it if missing. </summary>
        public static string GetDatabaseDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Database");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary> Get ".../AiDocumentsIndex" path. Creates it if missing. </summary>
        public static string GetAiDocumentsIndexDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "AiDocumentsIndex");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary>
        /// Get ".../AiModels" path form AppContext. This folder exists inside the app code.
        /// `npm run models` fetches it.
        /// </summary>
        public static string GetAiModelsPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "AiModels");
        }

        /// <summary> Get ".../OcrReaders" path. Creates it if missing. </summary>
        public static string GetOcrReadersPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "OcrReaders");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary> Get ".../Temp" path. Creates it if missing. </summary>
        public static string GetTempDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Temp");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary> Get ".../Export" path. Creates it if missing. </summary>
        public static string GetExportDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Export");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary> Get ".../Repository" path. Creates it if missing. </summary>
        public static string GetRepositoryPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Repository");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary> Get ".../Execution History" path. Creates it if missing. </summary>
        public static string GetExecutionHistoryDataPath()
        {
            string path = Path.Combine(_appDataPath, _appFolderName, "Execution History");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary> The folder one execution's screenshots go in, "{flowName} - {startedOn}", inside ".../Execution History". </summary>
        public static string GetExecutionFolderName(string flowName, DateTime startedOn)
        {
            string safeName = FileNameHelper.Clean(flowName, "Flow");

            return $"{safeName} - {startedOn:yyyy-MM-dd HH.mm.ss}";
        }
    }
}
