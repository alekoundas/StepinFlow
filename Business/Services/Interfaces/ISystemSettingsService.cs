using Model.Enums;
using Model.Models;
using Model.Structs;

namespace Business.Services.Interfaces
{
    public interface ISystemSettingsService
    {
        WindowSize GetMainWindowState();
        WindowSize GetSelectorWindowState();
        void SaveMainWindowState(WindowSize windowState);
        void SaveSelectorWindowState(WindowSize windowState);

        void UpdateSetting(AppSettingsEnum key, string value);
        AppSetting GetSetting(AppSettingsEnum key);
    }
}
