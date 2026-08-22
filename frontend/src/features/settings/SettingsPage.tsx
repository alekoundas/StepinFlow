import OcrLanguagesPanelComponent from "@/features/settings/components/OcrLanguagesPanelComponent";
import AppSettingsPanelComponent from "@/features/settings/components/AppSettingsPanelComponent";
import DiscordBotsPanelComponent from "@/features/settings/components/DiscordBotsPanelComponent";

export default function SettingsPage() {
  return (
    <div className="m-4 flex flex-column gap-4">
      <AppSettingsPanelComponent />
      <OcrLanguagesPanelComponent />
      <DiscordBotsPanelComponent />
    </div>
  );
}
