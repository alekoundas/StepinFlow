using Business.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Enums;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace StepinFlow.ViewModels.Pages
{
    public partial class SettingsVM : ObservableObject, INavigationAware
    {
        private readonly ISystemSettingsService _systemSettingsService;

        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty;
        [ObservableProperty]
        private bool _allowExecutionImageSave = true;
        [ObservableProperty]
        private double _executionImageQuality = 80d;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        public SettingsVM(ISystemSettingsService systemSettingsService)
        {
            _systemSettingsService = systemSettingsService;

            AllowExecutionImageSave = bool.Parse(_systemSettingsService.GetSetting(AppSettingsEnum.IS_EXECUTION_HISTORY_LOG_ENABLED).Value);
            ExecutionImageQuality = double.Parse(_systemSettingsService.GetSetting(AppSettingsEnum.EXECUTION_HISTORY_LOG_IMAGE_QUALITY).Value);
            ExecutionImageQuality = double.Parse(_systemSettingsService.GetSetting(AppSettingsEnum.EXECUTION_HISTORY_LOG_IMAGE_QUALITY).Value);
            CurrentTheme = ApplicationThemeManager.GetAppTheme();
        }


        [RelayCommand]
        private void OnSaveExecution()
        {
            _systemSettingsService.UpdateSetting(AppSettingsEnum.IS_EXECUTION_HISTORY_LOG_ENABLED, AllowExecutionImageSave.ToString());
            _systemSettingsService.UpdateSetting(AppSettingsEnum.EXECUTION_HISTORY_LOG_IMAGE_QUALITY, ExecutionImageQuality.ToString());
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;
                    _systemSettingsService.UpdateSetting(AppSettingsEnum.IS_THEME_DARK, "false");

                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;
                    _systemSettingsService.UpdateSetting(AppSettingsEnum.IS_THEME_DARK, "true");

                    break;
            }
        }

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}
