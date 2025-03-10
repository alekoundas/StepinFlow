using Business.Services.Interfaces;
using Model.Enums;
using Model.Models;
using Model.Structs;

namespace Business.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        public IDataService _dataService;

        private Dictionary<AppSettingsEnum, AppSetting> _appSettings;

        public SystemSettingsService(IDataService dataService)
        {
            _dataService = dataService;

            _appSettings = _dataService.AppSettings.ToList().ToDictionary(x => x.Key, x => x);
        }


        public WindowSize GetMainWindowState()
        {
            WindowSize windowState = new WindowSize();

            windowState.Left = double.Parse(_appSettings[AppSettingsEnum.MAIN_WINDOW_LEFT].Value);
            windowState.Top = double.Parse(_appSettings[AppSettingsEnum.MAIN_WINDOW_TOP].Value);
            windowState.Width = double.Parse(_appSettings[AppSettingsEnum.MAIN_WINDOW_WIDTH].Value);
            windowState.Height = double.Parse(_appSettings[AppSettingsEnum.MAIN_WINDOW_HEIGHT].Value);
            windowState.IsMaximized = bool.Parse(_appSettings[AppSettingsEnum.IS_MAIN_WINDOW_MAXIMIZED].Value);

            return windowState;
        }

        public WindowSize GetSelectorWindowState()
        {
            WindowSize windowState = new WindowSize();

            windowState.Left = double.Parse(_appSettings[AppSettingsEnum.SELECTOR_WINDOW_LEFT].Value);
            windowState.Top = double.Parse(_appSettings[AppSettingsEnum.SELECTOR_WINDOW_TOP].Value);
            windowState.Width = double.Parse(_appSettings[AppSettingsEnum.SELECTOR_WINDOW_WIDTH].Value);
            windowState.Height = double.Parse(_appSettings[AppSettingsEnum.SELECTOR_WINDOW_HEIGHT].Value);
            windowState.IsMaximized = bool.Parse(_appSettings[AppSettingsEnum.IS_SELECTOR_WINDOW_MAXIMIZED].Value);

            return windowState;
        }


        public void SaveMainWindowState(WindowSize windowState)
        {
            UpdateSetting(AppSettingsEnum.MAIN_WINDOW_TOP, windowState.Top.ToString());
            UpdateSetting(AppSettingsEnum.MAIN_WINDOW_LEFT, windowState.Left.ToString());
            UpdateSetting(AppSettingsEnum.MAIN_WINDOW_WIDTH, windowState.Width.ToString());
            UpdateSetting(AppSettingsEnum.MAIN_WINDOW_HEIGHT, windowState.Height.ToString());
            UpdateSetting(AppSettingsEnum.IS_MAIN_WINDOW_MAXIMIZED, windowState.IsMaximized.ToString());
        }

        public void SaveSelectorWindowState(WindowSize windowState)
        {
            UpdateSetting(AppSettingsEnum.SELECTOR_WINDOW_TOP, windowState.Top.ToString());
            UpdateSetting(AppSettingsEnum.SELECTOR_WINDOW_LEFT, windowState.Left.ToString());
            UpdateSetting(AppSettingsEnum.SELECTOR_WINDOW_WIDTH, windowState.Width.ToString());
            UpdateSetting(AppSettingsEnum.SELECTOR_WINDOW_HEIGHT, windowState.Height.ToString());
            UpdateSetting(AppSettingsEnum.IS_SELECTOR_WINDOW_MAXIMIZED, windowState.IsMaximized.ToString());
        }


        public void UpdateSetting(AppSettingsEnum key, string value)
        {
            AppSetting appSetting = _appSettings[key];
            appSetting.Value = value;

            _dataService.Update(appSetting);
        }

        public AppSetting GetSetting(AppSettingsEnum key)
        {
            AppSetting appSetting = _appSettings[key];
            return appSetting;
        }
    }
}
