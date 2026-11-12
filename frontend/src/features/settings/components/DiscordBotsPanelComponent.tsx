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
        text="Where a flow's Notify steps post, such as when an execution fails. Each bot is one Discord channel, with the name and picture its messages appear under."
        size="sm"
        color="secondary"
      />

      <div className="flex flex-column gap-1 mt-3">
        <LabelComponent
          text="Getting a webhook URL"
          weight="semibold"
          size="sm"
        />
        <ol className="m-0 pl-4 text-sm text-color-secondary line-height-3">
          <li>In Discord, click the gear beside the channel&apos;s name: Edit Channel.</li>
          <li>Integrations → Create Webhook, or View Webhooks → New Webhook if it has some already.</li>
          <li>Click the new webhook, then Copy Webhook URL.</li>
          <li>Here, Add Discord bot, and paste it into Webhook URL.</li>
        </ol>
        <LabelComponent
          text="It needs the Manage Webhooks permission on that server. Anyone with the URL can post to that channel, so keep it private."
          size="xs"
          color="secondary"
        />
      </div>

      <DiscordBotDataTableComponent className="mt-4" />
    </Panel>
  );
}
