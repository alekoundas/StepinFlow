import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import { DiscordBotDataTableComponent } from "@/features/discord-bot/components/DiscordBotDataTableComponent";

/**
 * Where notifications get set up. A Notify step picks one of these rather than carrying a URL of
 * its own, so changing where alerts land is one edit rather than one per step.
 */
export default function DiscordBotsPanelComponent() {
  return (
    <Panel header="Discord Bots">
      <LabelComponent
        text="Somewhere for a flow to shout when something breaks. Each one is a Discord channel plus the name and picture the messages appear under."
        size="sm"
        color="secondary"
      />

      <DiscordBotDataTableComponent className="mt-4" />
    </Panel>
  );
}
