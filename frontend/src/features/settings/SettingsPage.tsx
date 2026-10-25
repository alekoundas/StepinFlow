import OcrLanguagesPanelComponent from "@/features/settings/components/OcrLanguagesPanelComponent";
import AppSettingsPanelComponent from "@/features/settings/components/AppSettingsPanelComponent";
import DiscordBotsPanelComponent from "@/features/settings/components/DiscordBotsPanelComponent";
import HotkeysPanelComponent from "@/features/settings/components/HotkeysPanelComponent";
import AiSettingsPanelComponent from "@/features/settings/components/AiSettingsPanelComponent";
import RepositoryPanelComponent from "@/features/settings/components/RepositoryPanelComponent";

export default function SettingsPage() {
  return (
    <div className="m-4 flex flex-column gap-4">
      <AppSettingsPanelComponent
        header="Execution"
        description="What a run keeps behind it."
        keyPrefix="EXECUTION_"
      />

      <RepositoryPanelComponent />

      <AiSettingsPanelComponent />

      <OcrLanguagesPanelComponent />

      <HotkeysPanelComponent
        header="Recording keys"
        description="Pressed while you are recording another application, so these are matched system wide and never recorded. They still reach that application, so rebind any it uses."
        keyPrefix="RECORDING_HOTKEY_"
      />

      <HotkeysPanelComponent
        header="Debugger keys"
        description="Pressed while you are looking at the application being automated, not at StepinFlow, so these are matched system wide. That also means they still do whatever they normally do in that application - rebind any that clash with what a flow drives."
        keyPrefix="HOTKEY_"
      />
      <DiscordBotsPanelComponent />
    </div>
  );
}
