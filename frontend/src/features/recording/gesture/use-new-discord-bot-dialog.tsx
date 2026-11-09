import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { DiscordBotDto } from "@/shared/models/database/discord-bot-dto";
import DiscordBotFormComponent from "@/features/discord-bot/components/form/DiscordBotFormComponent";
import { useDiscordBotMutations } from "@/features/discord-bot/hooks/use-discord-bot";

const BOT_FORM_ID = "gesture-discord-bot-form";

/**
 * A new Discord bot, saved at once so a notification can use it by its id, as a Notify step's
 * form does. Where a dialog can open: the main window.
 */
export function useNewDiscordBotDialog(): (onCreated: (discordBotId: number) => void) => void {
  const { openForm, closeAll } = useDialogStore();
  const { createDiscordBotMutation } = useDiscordBotMutations();

  return (onCreated) =>
    openForm(BOT_FORM_ID, {
      headerText: "Add Discord bot",
      formId: BOT_FORM_ID,
      children: (
        <DiscordBotFormComponent
          formId={BOT_FORM_ID}
          formMode="ADD"
          defaultValues={new DiscordBotDto()}
          isFormInDialog={true}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();
            const newId = await createDiscordBotMutation.mutateAsync({ ...data, id: 0 });
            onCreated(newId);
          }}
        />
      ),
    });
}
