import OcrLanguagesPanelComponent from "@/features/settings/components/OcrLanguagesPanelComponent";
import AppSettingsPanelComponent from "@/features/settings/components/AppSettingsPanelComponent";
import DiscordBotsPanelComponent from "@/features/settings/components/DiscordBotsPanelComponent";
import HotkeysPanelComponent from "@/features/settings/components/HotkeysPanelComponent";
import AiSettingsPanelComponent from "@/features/settings/components/AiSettingsPanelComponent";

export default function SettingsPage() {
  return (
    <div className="m-4 flex flex-column gap-4">
      <AppSettingsPanelComponent
        header="Recording"
        description="How much of the screen is captured around the pointer each time you click while recording. Bigger gives the wizard more to crop a template from."
        keyPrefix="RECORDING_"
        numberSuffix=" px"
      />

      <AppSettingsPanelComponent
        header="Execution"
        description="What a run keeps behind it."
        keyPrefix="EXECUTION_"
      />

      <AiSettingsPanelComponent />

      <OcrLanguagesPanelComponent />
      <HotkeysPanelComponent />
      <DiscordBotsPanelComponent />
    </div>
  );
}
